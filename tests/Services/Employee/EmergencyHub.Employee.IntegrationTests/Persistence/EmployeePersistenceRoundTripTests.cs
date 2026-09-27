using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.Employee.IntegrationTests.Persistence;

// FR-06 공통 설정의 실제 DB 왕복(S03-T02 tester 인계, BL-082 S1 · testing-strategy.md Q8):
// 추가 → 커밋 → 새 쓰기 DbContext로 다시 읽기(private 생성자 구체화, 강타입 ID 변환, 감사 UTC, xmin 채움)와
// EmployeeReadRepository 프로젝션(e.Id == new EmployeeId(id) · e.Id.Value 번역, shadow 감사 시각)을 실제 PostgreSQL에서 한 번 실행한다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-06")]
public sealed class EmployeePersistenceRoundTripTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 10, 15, 30, 123, TimeSpan.Zero);

    // 서울 2027-01-01 08:30:00.456 = UTC 2026-12-31 23:30:00.456(연말 · 날짜 경계를 넘는 오프셋).
    private static readonly DateTimeOffset SeoulNewYearMorning = new(2027, 1, 1, 8, 30, 0, 456, TimeSpan.FromHours(9));

    // ---- 성공 ----

    [Fact]
    public async Task CommitThenReload_NewWriteDbContext_MaterializesThroughPrivateConstructorWithAuditAndVersion()
    {
        await using var services = Database.CreateServices(new EmployeeServicesOptions { TimeProvider = new FakeTimeProvider(Now) });
        var employee = new EmployeeBuilder().WithDisplayName("홍길동").WithEmail("Round.Trip@Example.com").Build();
        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();

        var loaded = await db.Set<Domain.Employees.Employee>().SingleAsync(e => e.Id == employee.Id, CancellationToken);

        employee.DomainEvents.Should().BeEmpty("커밋 성공 뒤 UnitOfWork가 이벤트를 비운다");
        (loaded.Id, loaded.DisplayName, loaded.Email, loaded.EmployeeStatus)
            .Should().Be((employee.Id, "홍길동", "round.trip@example.com", EmployeeStatus.Active));
        loaded.DomainEvents.Should().BeEmpty("구체화는 Register가 아니라 private 생성자를 쓴다");
        var entry = db.Entry(loaded);
        entry.Property<DateTimeOffset>(ShadowPropertyNames.CreatedAt).CurrentValue.Should().Be(Now).And.HaveOffset(TimeSpan.Zero);
        entry.Property<DateTimeOffset>(ShadowPropertyNames.UpdatedAt).CurrentValue.Should().Be(Now).And.HaveOffset(TimeSpan.Zero);
        entry.Property<uint>(ShadowPropertyNames.Version).CurrentValue.Should().Be(await ReadXminAsync(employee.Id.Value));
    }

    [Fact]
    public async Task GetByIdAsync_StoredEmployee_ProjectsIdValueAndShadowAuditColumnsOnReadConnection()
    {
        await using var services = Database.CreateServices(new EmployeeServicesOptions { TimeProvider = new FakeTimeProvider(Now) });
        var employee = new EmployeeBuilder().WithStatus(EmployeeStatus.Inactive).Build();
        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();

        var response = await scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().GetByIdAsync(employee.Id.Value, CancellationToken);

        response.Should().Be(new EmployeeResponse(employee.Id.Value, employee.DisplayName, employee.Email, EmployeeStatus.Inactive, Now, Now));
        response!.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    // ---- 실패 ----

    [Fact]
    public async Task GetByIdAsync_UnknownOrEmptyId_ReturnsNull()
    {
        await using var services = Database.CreateServices();
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>();

        var results = new[] { await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken), await repository.GetByIdAsync(Guid.Empty, CancellationToken) };

        results.Should().AllSatisfy(result => result.Should().BeNull());
    }

    // ---- 엣지 ----

    [Theory]
    [MemberData(nameof(BoundaryValues))]
    public async Task CommitThenRead_MaxLengthAndUnicodeValues_RoundTripUnchanged(string displayName, string email)
    {
        // 도메인 길이(UTF-16 코드 단위) 이하면 DB varchar(n)(문자 수)에도 들어간다. 이모지는 2단위 = 1문자.
        await using var services = Database.CreateServices();
        var employee = new EmployeeBuilder().WithDisplayName(displayName).WithEmail(email).Build();
        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();

        var response = await scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().GetByIdAsync(employee.Id.Value, CancellationToken);

        (response!.DisplayName, response.Email).Should().Be((displayName, email));
    }

    [Fact]
    public async Task Commit_TimeProviderAtPlusNineAndSeoulSession_StoresSameInstantAsUtc()
    {
        // S1: FakeTimeProvider가 +09:00 값을 돌려주고 EF 연결 세션이 Asia/Seoul이어도 timestamptz에는 같은 순간이 저장되고, 읽은 값의 오프셋은 0이다.
        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            TimeProvider = new FakeTimeProvider(SeoulNewYearMorning),
            WriteConnectionString = Database.WriteConnectionStringWith(builder => builder.Timezone = "Asia/Seoul"),
            ReadConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(Database.ReadConnectionString) { Timezone = "Asia/Seoul" }.ConnectionString,
        });
        var employee = new EmployeeBuilder().Build();

        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();

        var seoul = await ReadCreatedAtInSessionAsync(employee.Id.Value, "Asia/Seoul");
        var utc = await ReadCreatedAtInSessionAsync(employee.Id.Value, "UTC");
        seoul.Epoch.Should().Be(utc.Epoch);
        (seoul.Display, utc.Display).Should().Be(("2027-01-01 08:30:00.456+09", "2026-12-31 23:30:00.456+00"));
        await using var scope = services.CreateAsyncScope();
        var response = await scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().GetByIdAsync(employee.Id.Value, CancellationToken);
        response!.CreatedAt.Should().Be(SeoulNewYearMorning).And.HaveOffset(TimeSpan.Zero);
        response.CreatedAt.UtcDateTime.Should().Be(new DateTime(2026, 12, 31, 23, 30, 0, 456, DateTimeKind.Utc));
    }

    public static TheoryData<string, string> BoundaryValues() => new()
    {
        { new string('가', Domain.Employees.Employee.DisplayNameMaxLength), $"{new string('a', 64)}@{new string('b', Domain.Employees.Employee.EmailMaxLength - 64 - 1 - ".example.com".Length)}.example.com" },
        { string.Concat(Enumerable.Repeat("😀", Domain.Employees.Employee.DisplayNameMaxLength / 2)), "emoji@example.com" },
        { "한글 English 混合 😀", "mixed@example.com" },
    };

    private async Task<uint> ReadXminAsync(Guid id)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        return checked((uint)await connection.ScalarAsync<long>("SELECT xmin::text::bigint FROM employees WHERE id = $1", CancellationToken, id));
    }

    private async Task<(string Epoch, string Display)> ReadCreatedAtInSessionAsync(Guid id, string timeZone)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await connection.ExecuteSqlAsync($"SET TIME ZONE '{timeZone}'", CancellationToken);
        var epoch = await connection.ScalarAsync<string>("SELECT extract(epoch FROM created_at)::text FROM employees WHERE id = $1", CancellationToken, id);
        var display = await connection.ScalarAsync<string>("SELECT created_at::text FROM employees WHERE id = $1", CancellationToken, id);
        return (epoch, display);
    }
}
