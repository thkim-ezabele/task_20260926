using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Command 하나를 처리하는 Handler입니다. 디스패처(<see cref="ISender"/>)가 이 두 인자 형태로 해석합니다.
/// </summary>
/// <typeparam name="TCommand">처리할 Command 형식.</typeparam>
/// <typeparam name="TResponse">성공 값 형식.</typeparam>
/// <remarks>
/// <para>
/// 구현은 <c>internal sealed class</c>(primary constructor)이고 요청 하나만 처리합니다.
/// Handler는 저장하지 않습니다. 성공 결과면 트랜잭션 데코레이터가 <c>IUnitOfWork.CommitAsync</c>를 부릅니다(ADR-0014).
/// </para>
/// <para>
/// Handler는 <see cref="ISender"/>를 주입받지 않습니다. 중첩 Send는 커밋이 두 번 일어나므로 후속 처리는 도메인 이벤트로 잇습니다(ADR-0015).
/// </para>
/// </remarks>
public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>Command를 처리합니다.</summary>
    /// <param name="command">처리할 Command.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>예상 가능한 실패는 실패 <see cref="Result{T}"/>. 예상하지 못한 오류는 예외로 올려 보냅니다.</returns>
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
