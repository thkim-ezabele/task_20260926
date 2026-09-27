namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// 조회 요청(Query)입니다. 부작용이 없고 트랜잭션 없이 Read Repository로만 조회합니다(ADR-0007, ADR-0015).
/// </summary>
/// <typeparam name="TResponse">응답 형식(<c>record</c> DTO).</typeparam>
/// <remarks>구현 형식은 <c>sealed record</c>로 만듭니다.</remarks>
public interface IQuery<TResponse>;
