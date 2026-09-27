using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.ImplementationVisibility;

/// <summary>위반 예: 포트 구현이 public.</summary>
public sealed class PublicSampleClassifier : IExceptionClassifier
{
    /// <inheritdoc />
    public Error? Classify(Exception exception) => null;
}
