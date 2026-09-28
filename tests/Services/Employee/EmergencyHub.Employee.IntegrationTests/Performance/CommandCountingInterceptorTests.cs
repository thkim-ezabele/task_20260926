using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;

namespace EmergencyHub.Employee.IntegrationTests.Performance;

// S06-T06 도구(developer): EF 명령 수 세기 · 측정 조건(EnableSensitiveDataLogging 끔) 확인 도우미.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/NFR-02")]
public sealed class CommandCountingInterceptorTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // ---- 성공 ----

    [Fact]
    public async Task Commands_ApiHostUnitOfWorkCommit_CountsOneInsertCommand()
    {
        var counter = new CommandCountingInterceptor();
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteInterceptors = [counter] });

        (await EmployeeCommits.AddAndCommitAsync(factory.Services, new EmployeeBuilder().Build(), CancellationToken)).IsSuccess.Should().BeTrue();

        counter.InsertCommands.Should().ContainSingle().Which.InsertStatementCount.Should().Be(1);
        counter.InsertCommands[0].ParameterCount.Should().BePositive();
    }

    // ---- 실패 ----

    [Fact]
    public async Task Commands_FailedInsert_IsStillCounted()
    {
        var counter = new CommandCountingInterceptor();
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteInterceptors = [counter] });
        (await EmployeeCommits.AddAndCommitAsync(factory.Services, new EmployeeBuilder().WithEmail("dup@example.com").Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        counter.Clear();

        var second = await EmployeeCommits.AddAndCommitAsync(factory.Services, new EmployeeBuilder().WithEmail("dup@example.com").Build(), CancellationToken);

        second.IsFailure.Should().BeTrue("ux_employees_normalized_email 23505");
        counter.InsertCommands.Should().ContainSingle("실행 직전에 세므로 실패한 명령도 센다");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task IsSensitiveDataLoggingEnabled_DevelopmentTestHost_IsFalse()
    {
        await using var factory = new EmployeeApiFactory(Database);

        DbContextOptionsInspection.IsSensitiveDataLoggingEnabled(factory.Services).Should().BeFalse("성능 측정 · 개인정보 테스트 조건(NFR-04)");
    }
}
