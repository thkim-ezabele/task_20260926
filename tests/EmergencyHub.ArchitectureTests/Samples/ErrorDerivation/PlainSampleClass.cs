using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.ErrorDerivation;

/// <summary>규칙을 지킨 예: Error를 파생하지 않고 조합한다.</summary>
public sealed class PlainSampleClass
{
    /// <summary>오류.</summary>
    public Error Error { get; init; } = CommonErrors.NotFound;
}
