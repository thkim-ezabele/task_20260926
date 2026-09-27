using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Query 하나를 처리하는 Handler입니다. 트랜잭션 없이 읽기 전용 DbContext의 Read Repository로만 조회합니다(ADR-0007, ADR-0009).
/// </summary>
/// <typeparam name="TQuery">처리할 Query 형식.</typeparam>
/// <typeparam name="TResponse">응답 형식.</typeparam>
public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>Query를 처리합니다.</summary>
    /// <param name="query">처리할 Query.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>예상 가능한 실패(대상 없음 등)는 실패 <see cref="Result{T}"/>. 예상하지 못한 오류는 예외로 올려 보냅니다.</returns>
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
