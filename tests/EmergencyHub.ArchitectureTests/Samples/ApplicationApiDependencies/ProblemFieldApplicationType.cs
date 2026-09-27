using EmergencyHub.BuildingBlocks.Api.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationApiDependencies;

/// <summary>위반 예: BuildingBlocks.Api의 응답 형식을 만든다.</summary>
public sealed class ProblemFieldApplicationType
{
    /// <summary>표본 메서드.</summary>
    /// <returns>필드 오류 응답 항목.</returns>
    public static ProblemFieldError Create() => new(1001, "표본");
}
