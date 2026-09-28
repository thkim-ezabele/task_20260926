using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.ArchitectureTests.Samples.BuildingBlocksApiDependencies;

/// <summary>위반 예: 메서드 본문에서만 EF Core 예외 형식을 판별한다.</summary>
public sealed class DbUpdateCatchingApiType
{
    /// <summary>표본 메서드.</summary>
    /// <param name="exception">예외.</param>
    /// <returns>EF Core 저장 예외이면 true.</returns>
    public static bool IsDatabaseFailure(Exception exception) => exception is DbUpdateException;
}
