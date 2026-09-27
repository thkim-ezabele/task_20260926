using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.Pipeline;

/// <summary>
/// Command 검증 데코레이터입니다. 로깅 데코레이터 안쪽, 트랜잭션 데코레이터 바깥에서 실행합니다(ADR-0015, ADR-0018).
/// </summary>
/// <typeparam name="TCommand">Command 형식.</typeparam>
/// <typeparam name="TResponse">성공 값 형식.</typeparam>
/// <remarks>
/// Command의 Validator를 모두 실행해 실패가 하나라도 있으면 안쪽(트랜잭션 · Handler)을 부르지 않고
/// <see cref="Domain.Errors.ValidationError"/> 실패 결과를 돌려줍니다. Validator가 없으면 그대로 통과합니다.
/// 예외(<c>ValidateAndThrow</c>)는 쓰지 않습니다.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddBuildingBlocksApplication(S02-T03)이 open generic 데코레이터로 등록해 DI가 만든다(ADR-0015).")]
internal sealed class ValidationCommandHandlerDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var validationError = await RequestValidation.ValidateAsync(validators, command, cancellationToken);
        if (validationError is not null)
        {
            return Result.Failure<TResponse>(validationError);
        }

        return await inner.Handle(command, cancellationToken);
    }
}
