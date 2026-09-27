using EmergencyHub.Employee.IntegrationTests.FaultInjection;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T06: 테스트 인터셉터는 운영 등록이 만든 쓰기 DbContext 옵션에 덧붙인다(UseNpgsql 재호출 없음, EF8에 ConfigureDbContext 없음).
// 운영 인터셉터(감사)는 유지되고, 여러 번 부르면 이어 붙는다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-06")]
public sealed class DbContextInterceptorRegistrationTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // ---- 성공 ----

    [Fact]
    public async Task AddWriteDbContextInterceptors_AfterProductionRegistration_KeepsAuditInterceptor()
    {
        var probe = new TransactionProbeInterceptor();
        await using var services = Database.CreateServices(new EmployeeServicesOptions { WriteInterceptors = [probe] });

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        probe.Commits.Should().Be(1);
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>("SELECT count(*) FROM employees WHERE created_at = updated_at", CancellationToken)).Should().Be(1);
    }

    // ---- 실패 ----

    [Fact]
    public void AddWriteDbContextInterceptors_WithoutProductionRegistration_Throws()
    {
        var act = () => new ServiceCollection().AddWriteDbContextInterceptors(new TransactionProbeInterceptor());

        act.Should().Throw<InvalidOperationException>().WithMessage("*AddEmployeeInfrastructure*");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task AddWriteDbContextInterceptors_CalledTwice_AppendsBothInterceptors()
    {
        var first = new TransactionProbeInterceptor();
        var second = new TransactionProbeInterceptor();
        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            ConfigureServices = collection => collection.AddWriteDbContextInterceptors(first).AddWriteDbContextInterceptors(second),
        });

        await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        (first.Commits, second.Commits).Should().Be((1, 1));
    }
}
