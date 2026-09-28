using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Persistence;

// S05-T06 완료 조건 ①(PRD-002 FR-06): Write Repository의 DB 이메일 중복 사전 조회(람다 Contains → = ANY, 쓰기 연결)와 AddRange를 실제 PostgreSQL에서 확인한다.
// Repository는 넘겨받은 정규화 값을 그대로 비교한다(정규화 · Distinct · 분기는 Handler와 Email VO 몫). 사전 조회는 Handler 실행 시점(UoW 트랜잭션 밖)에 쓰기 연결로 간다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-06")]
public sealed class EmployeeRepositoryDatabaseTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // ---- 성공 ----

    [Fact]
    public async Task ListExistingNormalizedEmailsAsync_StoredAndNewValues_ReturnsOnlyStoredNormalizedValues()
    {
        await using var services = Database.CreateServices();
        await SeedAsync(services, "Hong@Example.com", "kim@example.com");
        await using var scope = services.CreateAsyncScope();

        var existing = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .ListExistingNormalizedEmailsAsync(["hong@example.com", "new@example.com", "kim@example.com"], CancellationToken);

        existing.Should().BeEquivalentTo("hong@example.com", "kim@example.com");
    }

    [Fact]
    public async Task AddRange_ThreeEmployees_StoresAllRowsInOneCommit()
    {
        await using var services = Database.CreateServices();
        await using var scope = services.CreateAsyncScope();
        var employees = Enumerable.Range(0, 3).Select(_ => new EmployeeBuilder().Build()).ToList();

        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().AddRange(employees);
        var result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await CountRowsAsync()).Should().Be(3);
    }

    // ---- 실패 ----

    [Fact]
    public async Task ListExistingNormalizedEmailsAsync_InputNotationInsteadOfNormalizedValue_DoesNotMatch()
    {
        // 입력 표기(대문자 포함)를 넘기면 찾지 못한다. 정규화(Email.NormalizedEmail)를 건너뛴 호출은 중복을 놓치므로 Handler가 정규화 값을 넘겨야 한다.
        await using var services = Database.CreateServices();
        await SeedAsync(services, "Hong@Example.com");
        await using var scope = services.CreateAsyncScope();

        var existing = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .ListExistingNormalizedEmailsAsync(["Hong@Example.com"], CancellationToken);

        existing.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRange_BatchWithSameNormalizedEmailTwice_Returns23001AndStoresNoRow()
    {
        // 사전 조회를 건너뛴 경합 경로: 배치 안 한 행이 유니크 인덱스를 어기면 23505 → 23001이고 배치 전체가 롤백된다("한 행이라도 실패하면 0건").
        await using var services = Database.CreateServices();
        await using var scope = services.CreateAsyncScope();
        Domain.Employees.Employee[] employees =
            [new EmployeeBuilder().Build(), new EmployeeBuilder().WithEmail("same@example.com").Build(), new EmployeeBuilder().WithEmail("Same@Example.com").Build()];

        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().AddRange(employees);
        var result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken);

        result.Error.Should().BeSameAs(EmployeeErrors.DuplicateEmail);
        (await CountRowsAsync()).Should().Be(0);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task ListExistingNormalizedEmailsAsync_EmptyInput_ReturnsEmpty()
    {
        await using var services = Database.CreateServices();
        await SeedAsync(services, "hong@example.com");
        await using var scope = services.CreateAsyncScope();

        var existing = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().ListExistingNormalizedEmailsAsync([], CancellationToken);

        existing.Should().BeEmpty();
    }

    [Fact]
    public async Task ListExistingNormalizedEmailsAsync_ReadConnectionUnusable_StillAnswersThroughWriteConnection()
    {
        // 사전 조회는 쓰기 연결(EmployeeDbContext)로 간다. 읽기 연결 비밀번호가 틀려도 결과가 나오면 읽기 연결을 쓰지 않은 것이다.
        await using (var seeding = Database.CreateServices())
        {
            await SeedAsync(seeding, "hong@example.com");
        }

        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            ReadConnectionString = new NpgsqlConnectionStringBuilder(Database.ReadConnectionString) { Password = "wrong-password" }.ConnectionString,
        });
        await using var scope = services.CreateAsyncScope();

        var existing = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().ListExistingNormalizedEmailsAsync(["hong@example.com"], CancellationToken);

        existing.Should().Equal("hong@example.com");
    }

    [Fact]
    public async Task ListExistingNormalizedEmailsAsync_SameValueRequestedTwice_ReturnsStoredValueOnce()
    {
        // 요청 안 중복 제거는 Handler 몫이지만, = ANY는 행 기준이라 같은 값을 두 번 넘겨도 저장된 행 하나만 돌려준다.
        await using var services = Database.CreateServices();
        await SeedAsync(services, "hong@example.com");
        await using var scope = services.CreateAsyncScope();

        var existing = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .ListExistingNormalizedEmailsAsync(["hong@example.com", "hong@example.com"], CancellationToken);

        existing.Should().Equal("hong@example.com");
    }

    private static async Task SeedAsync(IServiceProvider services, params string[] emails)
    {
        foreach (var email in emails)
        {
            (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithEmail(email).Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        }
    }

    private async Task<long> CountRowsAsync()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        return await connection.ScalarAsync<long>("SELECT count(*) FROM employees", CancellationToken);
    }
}
