using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Application.Pipeline;

/// <summary>
/// Command 파이프라인의 가장 바깥 데코레이터입니다. 결과(성공 / 실패 코드 · <c>ErrorType</c>)와 경과 시간을 한 줄로 남깁니다(ADR-0015).
/// </summary>
/// <typeparam name="TCommand">Command 형식.</typeparam>
/// <typeparam name="TResponse">성공 값 형식.</typeparam>
/// <remarks>
/// 가장 바깥에 있어 검증 실패와 커밋 단계의 실패(영속성 충돌)도 여기서 기록됩니다.
/// 예외는 잡지 않고 기록하지도 않습니다. 예외 로그는 전역 예외 처리기 한 곳에서만 남깁니다.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "PipelineDecorators 목록을 Infrastructure AddConventionalServices가 open generic TryDecorate로 등록해 DI가 만든다(ADR-0015, ADR-0017).")]
internal sealed class LoggingCommandHandlerDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<LoggingCommandHandlerDecorator<TCommand, TResponse>> logger,
    TimeProvider timeProvider) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private static readonly string RequestName = typeof(TCommand).Name;

    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();

        var result = await inner.Handle(command, cancellationToken);

        var elapsedMilliseconds = timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
        if (result.IsSuccess)
        {
            logger.CommandSucceeded(RequestName, elapsedMilliseconds);
        }
        else
        {
            logger.CommandFailed(RequestName, result.Error.Code, (short)result.Error.Type, elapsedMilliseconds);
        }

        return result;
    }
}
