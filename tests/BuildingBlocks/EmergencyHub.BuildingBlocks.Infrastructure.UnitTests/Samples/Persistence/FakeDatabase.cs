using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// DB 없이 UnitOfWork 경로를 확인하는 대역이다. 연결 열기 · 트랜잭션 시작 · 저장 · 커밋을 가로채 단계를 기록하고 실제 DB 호출을 건너뛴다.
/// 저장 · 커밋에서 던질 예외를 지정할 수 있다(실행 전략은 실제 Npgsql 재시도 전략이 그대로 돈다).
/// 실제 트랜잭션 · 롤백 · 재시도 지연은 S03-T05 통합 테스트가 확인한다.
/// </remarks>
public sealed class FakeDatabase : IDbConnectionInterceptor, IDbTransactionInterceptor, ISaveChangesInterceptor
{
    private readonly List<string> _steps = [];
    private readonly Queue<Exception> _commitFailures = new();

    public IReadOnlyList<string> Steps => _steps;

    public Func<Exception>? SaveFailure { get; set; }

    public void FailNextCommit(Exception exception) => _commitFailures.Enqueue(exception);

    public void Record(string step) => _steps.Add(step);

    public InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
        InterceptionResult.Suppress();

    public ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InterceptionResult.Suppress());

    public InterceptionResult<DbTransaction> TransactionStarting(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result) =>
        Begin(connection, eventData.IsolationLevel);

    public ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Begin(connection, eventData.IsolationLevel));

    public InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result) =>
        Commit();

    public ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Commit());

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) => Save();

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Save());

    private InterceptionResult<DbTransaction> Begin(DbConnection connection, IsolationLevel isolationLevel)
    {
        _steps.Add($"Begin:{isolationLevel}");
        return InterceptionResult<DbTransaction>.SuppressWithResult(new FakeDbTransaction(connection, isolationLevel, this));
    }

    private InterceptionResult Commit()
    {
        if (_commitFailures.TryDequeue(out var failure))
        {
            _steps.Add("CommitFailed");
            throw failure;
        }

        _steps.Add("Commit");
        return InterceptionResult.Suppress();
    }

    private InterceptionResult<int> Save()
    {
        _steps.Add("Save");
        if (SaveFailure is { } failure)
        {
            throw failure();
        }

        return InterceptionResult<int>.SuppressWithResult(0);
    }
}
