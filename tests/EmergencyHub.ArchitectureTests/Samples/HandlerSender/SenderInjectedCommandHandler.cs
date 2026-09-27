using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.HandlerSender;

/// <summary>위반 예: Command Handler가 ISender를 주입받는다(중첩 Send).</summary>
/// <param name="sender">디스패처.</param>
public sealed class SenderInjectedCommandHandler(ISender sender) : ICommandHandler<SampleCommand>
{
    /// <summary>디스패처.</summary>
    public ISender Sender { get; } = sender;

    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
}
