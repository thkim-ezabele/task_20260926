using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Services;
using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.ExplicitPorts;

/// <summary>위반 예: 명시 등록 포트 구현이 IService도 구현(자동 등록과 이중 등록).</summary>
public sealed class MarkedSampleClassifier : IExceptionClassifier, IService
{
    /// <inheritdoc />
    public Error? Classify(Exception exception) => null;
}
