using System.Data;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore.Storage;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

// S03-T06 장애 주입 도우미 스모크(testing-strategy.md "장애 주입"): 트랜잭션 인터셉터가 요청 격리 수준 · 커밋을 기록하고,
// 커밋 직전 일시 오류를 정한 횟수만 낸다(서버 커밋 전 차단이라 P2 서버 판정은 트리거가 주, 이것은 보조).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-05")]
public sealed class TransactionProbeInterceptorTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private static readonly DbRetryOptions FastRetry = new(maxRetryCount: 2, maxRetryDelay: TimeSpan.FromMilliseconds(10));

    // ---- 성공 ----

    [Fact]
    public async Task CommitAsync_ObserveOnly_RecordsRequestedReadCommittedAndOneCommit()
    {
        var probe = new TransactionProbeInterceptor();
        await using var services = Database.CreateServices(new EmployeeServicesOptions { WriteInterceptors = [probe] });

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        probe.StartedIsolationLevels.Should().Equal(IsolationLevel.ReadCommitted);
        (probe.CommitAttempts, probe.Commits).Should().Be((1, 1));
    }

    // ---- 실패 ----

    [Fact]
    public async Task CommitAsync_EveryCommitFails_ExceedsReducedRetryLimitWithoutCommit()
    {
        var probe = new TransactionProbeInterceptor(InjectedFailures.TransientTimeout, commitFailures: 100);
        await using var services = Database.CreateServices(new EmployeeServicesOptions { Retry = FastRetry, WriteInterceptors = [probe] });

        var act = () => EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        await act.Should().ThrowAsync<RetryLimitExceededException>();
        (probe.CommitAttempts, probe.Commits).Should().Be((FastRetry.MaxRetryCount + 1, 0));
    }

    // ---- 엣지 ----

    [Fact]
    public async Task CommitAsync_OneCommitFailure_RetriesWholeTransactionAndCommitsOnce()
    {
        var probe = new TransactionProbeInterceptor(InjectedFailures.TransientTimeout, commitFailures: 1);
        await using var services = Database.CreateServices(new EmployeeServicesOptions { Retry = FastRetry, WriteInterceptors = [probe] });

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        probe.StartedIsolationLevels.Should().Equal(IsolationLevel.ReadCommitted, IsolationLevel.ReadCommitted);
        (probe.CommitAttempts, probe.Commits).Should().Be((2, 1));
    }
}
