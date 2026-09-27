using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.Pipeline;

/// <summary>
/// Query 검증 데코레이터입니다. 로깅 데코레이터 안쪽에서 실행합니다(ADR-0015, ADR-0018).
/// </summary>
/// <typeparam name="TQuery">Query 형식.</typeparam>
/// <typeparam name="TResponse">응답 형식.</typeparam>
/// <remarks>
/// 실패가 하나라도 있으면 Handler를 부르지 않고 <see cref="Domain.Errors.ValidationError"/> 실패 결과를 돌려줍니다.
/// Validator가 없으면 그대로 통과합니다.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddBuildingBlocksApplication(S02-T03)이 open generic 데코레이터로 등록해 DI가 만든다(ADR-0015).")]
internal sealed class ValidationQueryHandlerDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var validationError = await RequestValidation.ValidateAsync(validators, query, cancellationToken);
        if (validationError is not null)
        {
            return Result.Failure<TResponse>(validationError);
        }

        return await inner.Handle(query, cancellationToken);
    }
}
