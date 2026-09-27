using System.Data.Common;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore.Storage;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

// S03-T06 장애 주입 도우미 스모크(testing-strategy.md "장애 주입"): 명령 인터셉터가 정한 횟수만 일시 오류를 내고,
// 재시도 한도는 DbRetryOptions로 줄여 등록한다(시도 수 = MaxRetryCount + 1).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-05")]
public sealed class CommandFaultInterceptorTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private static readonly DbRetryOptions FastRetry = new(maxRetryCount: 2, maxRetryDelay: TimeSpan.FromMilliseconds(10));

    // ---- 성공 ----

    [Fact]
    public async Task CommitAsync_OneTransientFailure_RetriesToSuccess()
    {
        var fault = new CommandFaultInterceptor(InjectedFailures.SerializationFailure, failures: 1, IsEmployeeInsert);
        await using var services = Database.CreateServices(new EmployeeServicesOptions { Retry = FastRetry, WriteInterceptors = [fault] });

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        fault.Attempts.Should().Be(2);
    }

    // ---- 실패 ----

    [Fact]
    public async Task CommitAsync_AlwaysFailing_ExceedsReducedRetryLimit()
    {
        var fault = new CommandFaultInterceptor(InjectedFailures.TransientTimeout, failures: int.MaxValue, IsEmployeeInsert);
        await using var services = Database.CreateServices(new EmployeeServicesOptions { Retry = FastRetry, WriteInterceptors = [fault] });

        var act = () => EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        await act.Should().ThrowAsync<RetryLimitExceededException>();
        fault.Attempts.Should().Be(FastRetry.MaxRetryCount + 1);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task CommitAsync_ZeroFailures_PassesThroughAndCountsAttempt()
    {
        var fault = new CommandFaultInterceptor(InjectedFailures.SerializationFailure, failures: 0, IsEmployeeInsert);
        await using var services = Database.CreateServices(new EmployeeServicesOptions { WriteInterceptors = [fault] });

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        fault.Attempts.Should().Be(1);
    }

    [Fact]
    public void Constructor_NegativeFailures_Throws()
    {
        var act = () => new CommandFaultInterceptor(InjectedFailures.SerializationFailure, failures: -1);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("failures");
    }

    private static bool IsEmployeeInsert(DbCommand command) =>
        command.CommandText.Contains("INSERT INTO employees", StringComparison.Ordinal);
}
