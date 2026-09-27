using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Domain.Results;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Command 형식이 확정된 강타입 호출기입니다. <see cref="RequestInvokerCache"/>가 형식마다 한 번만 만듭니다.
/// </summary>
/// <typeparam name="TCommand">Command 형식.</typeparam>
/// <typeparam name="TResponse">성공 값 형식.</typeparam>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "RequestInvokerCache가 MakeGenericType + Activator로 만든다(ADR-0015).")]
internal sealed class CommandInvoker<TCommand, TResponse> : CommandInvoker<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override async Task<Result<TResponse>> InvokeAsync(
        ICommand<TResponse> command,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService<ICommandHandler<TCommand, TResponse>>()
            ?? throw MissingHandler.Create(typeof(TCommand), typeof(ICommandHandler<TCommand, TResponse>));

        return await handler.Handle((TCommand)command, cancellationToken);
    }
}
