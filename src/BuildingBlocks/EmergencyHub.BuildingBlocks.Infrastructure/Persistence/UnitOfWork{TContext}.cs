using System.Data;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Events;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// 서비스 쓰기 DbContext 하나에 묶인 <see cref="IUnitOfWork"/> 구현입니다(ADR-0014, database.md "UnitOfWork 커밋 순서").
/// </summary>
/// <typeparam name="TContext">서비스 쓰기 DbContext.</typeparam>
/// <remarks>
/// <list type="number">
/// <item><description>
/// 실행 전략 안(재시도 단위): <c>BeginTransactionAsync(ReadCommitted)</c> → <c>SaveChangesAsync(acceptAllChangesOnSuccess: false)</c>
/// → <see cref="IPreCommitHook"/>(Outbox 확장 지점, 재시도 때 다시 호출되므로 멱등이어야 함) → 커밋. 트랜잭션은 <c>await using</c>이라 예외가 전략 밖으로 나가기 전에 롤백됩니다.
/// </description></item>
/// <item><description>
/// 전략 밖, 커밋 성공 뒤: <see cref="IHasDomainEvents"/> 엔트리를 먼저 모은 다음 <c>AcceptAllChanges</c> → 모은 Aggregate의 <c>ClearDomainEvents</c>
/// (<c>AcceptAllChanges</c>가 Deleted를 Detached로 빼므로 순서를 바꾸면 삭제된 Aggregate의 이벤트가 남음).
/// </description></item>
/// <item><description>
/// 예외 변환은 전략 <b>바깥</b>에서 합니다(안에서 잡으면 실행 전략이 일시 오류를 보지 못해 재시도하지 않음). 변환 대상이 아니면 같은 예외를 그대로 올립니다.
/// </description></item>
/// </list>
/// 실패(변환된 결과 · 예외) 뒤에는 추적 상태 · 도메인 이벤트를 그대로 둡니다. 스코프 하나 = Command 하나가 전제라 실패한 DbContext로 다른 Command를 커밋하지 않습니다.
/// </remarks>
internal sealed class UnitOfWork<TContext>(
    TContext db,
    UniqueConstraintErrorRegistry uniqueConstraintErrors,
    IEnumerable<IPreCommitHook> preCommitHooks,
    ILogger<UnitOfWork<TContext>> logger) : IUnitOfWork
    where TContext : WriteDbContextBase
{
    private readonly IPreCommitHook[] _preCommitHooks = [.. preCommitHooks];

    public async Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(this, static (unitOfWork, token) => unitOfWork.SaveAndCommitAsync(token), cancellationToken);
        }
        catch (DbUpdateException exception) when (PersistenceExceptionTranslator.Translate(exception, uniqueConstraintErrors) is { } translation)
        {
            Log(translation);
            return Result.Failure(translation.Error);
        }

        CompleteCommit();
        return Result.Success();
    }

    private async Task SaveAndCommitAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);

        foreach (var hook in _preCommitHooks)
        {
            await hook.BeforeCommitAsync(db, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private void CompleteCommit()
    {
        var aggregates = db.ChangeTracker.Entries().Select(entry => entry.Entity).OfType<IHasDomainEvents>().ToList();
        db.ChangeTracker.AcceptAllChanges();

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }

    private void Log(PersistenceExceptionTranslation translation)
    {
        var entityTypes = string.Join(", ", translation.EntityTypeNames);

        switch (translation.Kind)
        {
            case PersistenceFailureKind.MappedUniqueViolation:
                logger.UniqueConstraintViolationMapped(translation.ConstraintName, translation.SqlState, entityTypes, translation.Error.Code);
                break;
            case PersistenceFailureKind.UnmappedUniqueViolation:
                logger.UniqueConstraintViolationUnmapped(translation.ConstraintName, translation.SqlState, entityTypes, translation.Error.Code);
                break;
            case PersistenceFailureKind.ConcurrencyConflict:
                logger.ConcurrencyConflictDetected(entityTypes, translation.Error.Code);
                break;
        }
    }
}
