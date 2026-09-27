using EmergencyHub.BuildingBlocks.Api.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.MigrationServiceDependencies;

/// <summary>위반 예: MigrationService가 BuildingBlocks.Api 형식을 속성으로 쓴다.</summary>
public sealed class ProblemFieldMigrationType
{
    /// <summary>필드 오류 응답 항목.</summary>
    public ProblemFieldError? Field { get; init; }
}
