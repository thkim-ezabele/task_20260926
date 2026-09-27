using System.Diagnostics.CodeAnalysis;
using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.ImplementationVisibility;

/// <summary>규칙을 지킨 예: 포트 구현이 internal sealed.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = SampleSuppressions.MetadataOnly)]
internal sealed class InternalSampleClassifier : IExceptionClassifier
{
    /// <inheritdoc />
    public Error? Classify(Exception exception) => null;
}
