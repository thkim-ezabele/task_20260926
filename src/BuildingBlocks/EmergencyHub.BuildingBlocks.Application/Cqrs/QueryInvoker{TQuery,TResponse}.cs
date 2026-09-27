using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Domain.Results;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Query 형식이 확정된 강타입 호출기입니다. <see cref="RequestInvokerCache"/>가 형식마다 한 번만 만듭니다.
/// </summary>
/// <typeparam name="TQuery">Query 형식.</typeparam>
/// <typeparam name="TResponse">응답 형식.</typeparam>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "RequestInvokerCache가 MakeGenericType + Activator로 만든다(ADR-0015).")]
internal sealed class QueryInvoker<TQuery, TResponse> : QueryInvoker<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override async Task<Result<TResponse>> InvokeAsync(
        IQuery<TResponse> query,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService<IQueryHandler<TQuery, TResponse>>()
            ?? throw MissingHandler.Create(typeof(TQuery), typeof(IQueryHandler<TQuery, TResponse>));

        return await handler.Handle((TQuery)query, cancellationToken);
    }
}
