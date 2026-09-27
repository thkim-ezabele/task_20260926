using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.HandlerSender;

/// <summary>위반 예: Query Handler가 ISender를 주입받는다.</summary>
/// <param name="sender">디스패처.</param>
public sealed class SenderInjectedQueryHandler(ISender sender) : IQueryHandler<SampleQuery, Unit>
{
    /// <summary>디스패처.</summary>
    public ISender Sender { get; } = sender;

    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
}
