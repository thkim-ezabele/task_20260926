using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationApiDependencies;

/// <summary>규칙을 지킨 예: Result로 실패를 돌려주고 HTTP 형식은 모른다.</summary>
public sealed class ResultReturningApplicationType
{
    /// <summary>표본 메서드.</summary>
    /// <returns>실패 Result.</returns>
    public static Result Fail() => CommonErrors.NotFound;
}
