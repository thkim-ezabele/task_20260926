namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// UnitOfWork 커밋 직전(같은 트랜잭션, <c>SaveChanges</c> 뒤)에 실행되는 확장 지점입니다. Outbox 기록 자리입니다(ADR-0014, ADR-0023).
/// </summary>
/// <remarks>
/// <para>
/// <b>멱등이어야 합니다.</b> 실행 전략이 일시 오류로 재시도하면 트랜잭션 · <c>SaveChanges</c>와 함께 이 확장 지점도 다시 호출됩니다
/// (같은 엔트리를 두 번 Add하지 않음, database.md "재시도 때 상태").
/// </para>
/// <para>
/// 지금은 구현이 없습니다(Outbox 도입 보류, ADR-0023). DI 자동 등록 마커를 상속하지 않으므로, 도입할 때 서비스가 Scoped로 명시 등록하고
/// UnitOfWork가 등록된 순서대로 부릅니다. 없으면 건너뜁니다.
/// </para>
/// </remarks>
public interface IPreCommitHook
{
    /// <summary>커밋 직전에 실행합니다. 재시도 때 다시 호출될 수 있으므로 멱등이어야 합니다.</summary>
    /// <param name="context">변경을 저장한 쓰기 DbContext(현재 트랜잭션이 열려 있음).</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>완료 작업.</returns>
    Task BeforeCommitAsync(WriteDbContextBase context, CancellationToken cancellationToken);
}
