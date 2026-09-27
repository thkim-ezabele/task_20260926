using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Application.Pipeline;

/// <summary>
/// Query 파이프라인의 가장 바깥 데코레이터입니다. 결과(성공 / 실패 코드 · <c>ErrorType</c>)와 경과 시간을 한 줄로 남깁니다(ADR-0015).
/// </summary>
/// <typeparam name="TQuery">Query 형식.</typeparam>
/// <typeparam name="TResponse">응답 형식.</typeparam>
/// <remarks>예외는 잡지 않고 기록하지도 않습니다. 규칙은 <see cref="LoggingCommandHandlerDecorator{TCommand, TResponse}"/>와 같습니다.</remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "PipelineDecorators 목록을 Infrastructure AddConventionalServices가 open generic TryDecorate로 등록해 DI가 만든다(ADR-0015, ADR-0017).")]
internal sealed class LoggingQueryHandlerDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    ILogger<LoggingQueryHandlerDecorator<TQuery, TResponse>> logger,
    TimeProvider timeProvider) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private static readonly string RequestName = typeof(TQuery).Name;

    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();

        var result = await inner.Handle(query, cancellationToken);

        var elapsedMilliseconds = timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
        if (result.IsSuccess)
        {
            logger.QuerySucceeded(RequestName, elapsedMilliseconds);
        }
        else
        {
            logger.QueryFailed(RequestName, result.Error.Code, (short)result.Error.Type, elapsedMilliseconds);
        }

        return result;
    }
}
