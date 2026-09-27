using EmergencyHub.BuildingBlocks.Api.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.InfrastructureApiDependencies;

/// <summary>위반 예: BuildingBlocks.Api 형식을 쓴다.</summary>
public sealed class ProblemFieldInfrastructureType
{
    /// <summary>필드 오류 응답 항목.</summary>
    public ProblemFieldError? Field { get; init; }
}
