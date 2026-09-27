using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.MigrationServiceDependencies;

/// <summary>위반 예: MigrationService가 메서드 본문에서 BuildingBlocks.Api 변환을 호출한다(시그니처는 Domain 형식만).</summary>
public sealed class ProblemResultMigrationType
{
    /// <summary>오류를 HTTP 응답으로 바꾼다.</summary>
    /// <param name="error">오류.</param>
    /// <returns>응답 결과.</returns>
    public object Map(Error error) => error.ToProblemResult();
}
