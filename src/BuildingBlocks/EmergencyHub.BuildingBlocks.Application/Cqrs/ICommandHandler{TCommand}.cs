namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// 반환 값이 없는 Command(<see cref="ICommand"/>)의 Handler 편의 인터페이스입니다. <c>ICommandHandler&lt;TCommand, Unit&gt;</c>과 같습니다.
/// </summary>
/// <typeparam name="TCommand">처리할 Command 형식.</typeparam>
/// <remarks>
/// DI 등록과 데코레이터 적용은 두 인자 형태(<see cref="ICommandHandler{TCommand, TResponse}"/>)로만 합니다(ADR-0015, ADR-0017).
/// </remarks>
public interface ICommandHandler<TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand;
