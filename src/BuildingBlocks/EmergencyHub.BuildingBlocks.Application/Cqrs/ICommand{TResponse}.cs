namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// 상태를 바꾸는 요청(Command)입니다. 처리 결과는 <c>Result&lt;TResponse&gt;</c>이고, 트랜잭션 데코레이터가 Command에만 적용됩니다(ADR-0015).
/// </summary>
/// <typeparam name="TResponse">성공 값 형식. 생성한 ID 정도만 돌려줍니다(coding-conventions "CQRS 규칙").</typeparam>
/// <remarks>구현 형식은 <c>sealed record</c>로 만듭니다.</remarks>
public interface ICommand<TResponse>;
