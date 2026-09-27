using System.Diagnostics.CodeAnalysis;
using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.ImplementationVisibility;

/// <summary>규칙을 지킨 예: Handler가 internal sealed.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = SampleSuppressions.MetadataOnly)]
internal sealed class InternalSampleCommandHandler : ICommandHandler<SampleCommand>
{
    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
}
