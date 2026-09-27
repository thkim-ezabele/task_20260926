using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.Employee.IntegrationTests.Persistence;

// FR-08 인수 조건 "이메일 중복 → 충돌", "동시성 충돌 → 충돌 Result(인위적 재현)"의 영속성 경로(BL-081 P4 · P5, testing-strategy.md Q5 · Q9).
// P4: Handler 사전 검사를 건너뛰고 쓰기 DbContext에 바로 Add → CommitAsync → 서버 23505의 ConstraintName으로 UnitOfWork가 변환한다(실제 DB 이름 = 상수).
// P5: 스코프 2개(= Command 2개)가 같은 행을 읽고 Deactivate → 먼저 커밋한 쪽 Success, 나중 쪽 xmin 불일치 → 3001.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-08")]
public sealed class UnitOfWorkConflictTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string UnitOfWorkCategoryPrefix = "EmergencyHub.BuildingBlocks.Infrastructure.Persistence.UnitOfWork";
    private const string PrimaryKeyName = "pk_employees";

    private static readonly DateTimeOffset RegisteredAt = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);

    // ---- 성공 ----

    [Fact]
    public async Task CommitAsync_TwoScopesDeactivateSameEmployee_FirstSucceedsAndUpdatesXminAndUpdatedAtOnly()
    {
        var time = new FakeTimeProvider(RegisteredAt);
        await using var services = Database.CreateServices(new EmployeeServicesOptions { TimeProvider = time });
        var id = await SeedAsync(services);
        var before = await ReadRowAsync(id);
        await using var first = services.CreateAsyncScope();
        await using var second = services.CreateAsyncScope();
        await LoadAndDeactivateAsync(first, id);
        await LoadAndDeactivateAsync(second, id);
        time.Advance(TimeSpan.FromMinutes(5));

        var result = await first.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var after = await ReadRowAsync(id);
        after.Version.Should().NotBe(before.Version);
        after.Status.Should().Be((short)EmployeeStatus.Inactive);
        after.CreatedAt.Should().Be(RegisteredAt);
        after.UpdatedAt.Should().Be(RegisteredAt.AddMinutes(5));
    }

    // ---- 실패 ----

    [Fact]
    public async Task CommitAsync_SecondScopeDeactivatesWithStaleXmin_Returns3001AndLogs203AtDebug()
    {
        await using var services = Database.CreateServices();
        var id = await SeedAsync(services);
        await using var first = services.CreateAsyncScope();
        await using var second = services.CreateAsyncScope();
        await LoadAndDeactivateAsync(first, id);
        await LoadAndDeactivateAsync(second, id);
        (await first.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken)).IsSuccess.Should().BeTrue();
        var committed = await ReadRowAsync(id);
        services.GetFakeLogCollector().Clear();

        var result = await second.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken);

        result.Error.Code.Should().Be(CommonErrors.ConcurrencyConflict.Code);
        var log = UnitOfWorkLogs(services).Should().ContainSingle().Subject;
        (log.Id.Id, log.Level).Should().Be((203, LogLevel.Debug));
        log.GetStructuredStateValue("EntityTypes").Should().Be(nameof(Domain.Employees.Employee));
        (await ReadRowAsync(id)).Should().Be(committed, "나중 커밋은 롤백되어 행이 바뀌지 않는다");
    }

    [Theory]
    [InlineData("dup@example.com", "dup@example.com")]
    [InlineData("dup@example.com", "  DUP@Example.COM ")]
    public async Task CommitAsync_SameNormalizedEmailWithoutPreCheck_Returns23001FromEmailUniqueIndex(string storedEmail, string newEmail)
    {
        // 대소문자 · 앞뒤 공백 차이는 도메인이 정규화하므로 DB 유니크 인덱스(일반 인덱스, lower() 식 아님)에서 같은 값으로 걸린다.
        await using var services = Database.CreateServices();
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithEmail(storedEmail).Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        services.GetFakeLogCollector().Clear();

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithEmail(newEmail).Build(), CancellationToken);

        result.Error.Should().BeSameAs(EmployeeErrors.DuplicateEmail);
        var log = UnitOfWorkLogs(services).Should().ContainSingle().Subject;
        (log.Id.Id, log.Level).Should().Be((201, LogLevel.Debug));
        log.GetStructuredStateValue("ConstraintName").Should().Be(EmployeeDbNames.EmailUniqueIndex.Value);
        log.GetStructuredStateValue("SqlState").Should().Be("23505");
        log.GetStructuredStateValue("ErrorCode").Should().Be("23001");
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>("SELECT count(*) FROM employees", CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task CommitAsync_SameIdDifferentEmail_Returns3003FromPrimaryKeyAndLogs202AtWarning()
    {
        // 매핑 없는 유니크 위반(pk_employees)은 공통 3003. 실제 DB의 PK 이름이 규칙 이름(pk_<table>)과 같아야 로그 · 판정이 맞는다.
        await using var services = Database.CreateServices();
        var id = await SeedAsync(services);
        services.GetFakeLogCollector().Clear();

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithId(id).Build(), CancellationToken);

        result.Error.Code.Should().Be(CommonErrors.UniqueConstraintViolated.Code);
        var log = UnitOfWorkLogs(services).Should().ContainSingle().Subject;
        (log.Id.Id, log.Level).Should().Be((202, LogLevel.Warning));
        log.GetStructuredStateValue("ConstraintName").Should().Be(PrimaryKeyName);
        log.GetStructuredStateValue("ErrorCode").Should().Be("3003");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task CommitAsync_DeactivateAlreadyInactiveEmployee_WritesNothingAndKeepsXminAndUpdatedAt()
    {
        // 같은 상태로 전이: Deactivate는 멱등이라 변경 감지가 없고 UPDATE가 나가지 않는다(xmin · updated_at 그대로).
        var time = new FakeTimeProvider(RegisteredAt);
        await using var services = Database.CreateServices(new EmployeeServicesOptions { TimeProvider = time });
        var id = new EmployeeBuilder().Build().Id;
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithId(id).WithStatus(EmployeeStatus.Inactive).Build(), CancellationToken))
            .IsSuccess.Should().BeTrue();
        var before = await ReadRowAsync(id);
        await using var scope = services.CreateAsyncScope();
        await LoadAndDeactivateAsync(scope, id);
        time.Advance(TimeSpan.FromMinutes(5));

        var result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await ReadRowAsync(id)).Should().Be(before);
    }

    private static IReadOnlyList<FakeLogRecord> UnitOfWorkLogs(ServiceProvider services) =>
        [.. services.GetFakeLogCollector().GetSnapshot().Where(record => record.Category?.StartsWith(UnitOfWorkCategoryPrefix, StringComparison.Ordinal) == true)];

    private static async Task<EmployeeId> SeedAsync(ServiceProvider services)
    {
        var employee = new EmployeeBuilder().Build();
        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        return employee.Id;
    }

    // IEmployeeRepository에 조회 메서드가 없어 쓰기 DbContext Set<Employee>()로 추적 로드한다(private 생성자 구체화, S03-T01 인계).
    private static async Task LoadAndDeactivateAsync(AsyncServiceScope scope, EmployeeId id)
    {
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        var employee = await db.Set<Domain.Employees.Employee>().SingleAsync(e => e.Id == id, CancellationToken);
        employee.Deactivate();
    }

    private async Task<EmployeeRow> ReadRowAsync(EmployeeId id)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await using var command = new Npgsql.NpgsqlCommand(
            "SELECT xmin::text::bigint, employee_status, created_at, updated_at FROM employees WHERE id = $1", connection)
        {
            Parameters = { new Npgsql.NpgsqlParameter { Value = id.Value } },
        };
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        (await reader.ReadAsync(CancellationToken)).Should().BeTrue();

        return new EmployeeRow(
            reader.GetInt64(0),
            reader.GetInt16(1),
            reader.GetFieldValue<DateTimeOffset>(2),
            reader.GetFieldValue<DateTimeOffset>(3));
    }

    private sealed record EmployeeRow(long Version, short Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
}
