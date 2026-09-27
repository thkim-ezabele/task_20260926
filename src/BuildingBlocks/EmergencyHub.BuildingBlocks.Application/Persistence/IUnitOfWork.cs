using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.Persistence;

/// <summary>
/// Command 하나의 변경을 한 트랜잭션으로 커밋합니다. 트랜잭션 데코레이터만 부르고 Handler · Repository는 부르지 않습니다(ADR-0014, ADR-0015).
/// </summary>
/// <remarks>
/// <para>
/// 저장 · 트랜잭션 시작을 따로 공개하지 않습니다(<c>SaveChangesAsync</c> · <c>BeginTransaction</c> 없음). Handler는 저장하지 않으며,
/// 실행 전략 · 트랜잭션 · <c>SaveChanges</c> · 커밋 순서는 모두 구현(BuildingBlocks.Infrastructure) 안에서 처리합니다.
/// </para>
/// <para>
/// DI 자동 등록 마커(<c>IService</c>)를 상속하지 않습니다. 구현은 Infrastructure의 UnitOfWork 등록 확장이 DbContext와 함께 등록합니다(S02-T07).
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>현재 스코프의 변경을 한 트랜잭션으로 저장하고 커밋합니다.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>
    /// 성공 결과, 또는 예상 가능한 영속성 충돌(동시 수정 3001, 유니크 제약 위반 등)을 바꾼 실패 결과.
    /// <b>실패 결과를 돌려줄 때는 트랜잭션이 이미 롤백된 상태입니다.</b> 호출자는 롤백을 따로 하지 않고 결과를 그대로 전달합니다.
    /// </returns>
    /// <exception cref="OperationCanceledException">취소된 경우.</exception>
    /// <remarks>
    /// 예상하지 못한 영속성 오류(변환 대상이 아닌 DB 예외, 재시도 한도 초과 등)는 결과로 바꾸지 않고 예외로 올려 보냅니다.
    /// 영속성 예외 형식은 Application으로 새지 않습니다(ADR-0014).
    /// </remarks>
    Task<Result> CommitAsync(CancellationToken cancellationToken);
}
