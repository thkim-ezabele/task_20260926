using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>
/// 트랜잭션 시작 · 커밋을 관찰하고, 정한 횟수만큼 커밋 직전에 예외를 던지는 인터셉터입니다(testing-strategy.md "장애 주입").
/// </summary>
/// <remarks>
/// 시작 때 기록하는 격리 수준은 EF가 <b>요청한</b> 값입니다(서버 값이 아님). P1의 주 판정은 관찰 트리거(<see cref="TestTriggers.CreateIsolationProbeAsync"/>)이고 이것은 보조입니다.
/// 커밋 직전 예외는 서버 커밋 전에 끊는 것이라 P2(서버 커밋 시점 40001)의 대안으로만 씁니다.
/// </remarks>
/// <param name="commitFailure">커밋 직전에 던질 예외를 만드는 함수. <see langword="null"/>이면 관찰만 합니다.</param>
/// <param name="commitFailures">커밋 실패 횟수(0 이상).</param>
public sealed class TransactionProbeInterceptor(Func<Exception>? commitFailure = null, int commitFailures = 0) : DbTransactionInterceptor
{
    private readonly ConcurrentQueue<IsolationLevel> _startedIsolationLevels = new();
    private int _remainingCommitFailures = commitFailures >= 0 ? commitFailures : throw new ArgumentOutOfRangeException(nameof(commitFailures));
    private int _commitAttempts;
    private int _commits;

    /// <summary>시작한 트랜잭션의 요청 격리 수준(시작 순서)입니다.</summary>
    public IReadOnlyList<IsolationLevel> StartedIsolationLevels => [.. _startedIsolationLevels];

    /// <summary>커밋 시도 수(주입한 실패 포함)입니다.</summary>
    public int CommitAttempts => Volatile.Read(ref _commitAttempts);

    /// <summary>서버 커밋이 끝난 수입니다.</summary>
    public int Commits => Volatile.Read(ref _commits);

    /// <inheritdoc/>
    public override InterceptionResult<DbTransaction> TransactionStarting(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result)
    {
        _startedIsolationLevels.Enqueue(eventData.IsolationLevel);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    {
        _startedIsolationLevels.Enqueue(eventData.IsolationLevel);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
    {
        BeforeCommit();
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        BeforeCommit();
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) =>
        Interlocked.Increment(ref _commits);

    /// <inheritdoc/>
    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _commits);
        return Task.CompletedTask;
    }

    private void BeforeCommit()
    {
        Interlocked.Increment(ref _commitAttempts);

        if (commitFailure is not null && Interlocked.Decrement(ref _remainingCommitFailures) >= 0)
        {
            throw commitFailure();
        }
    }
}
