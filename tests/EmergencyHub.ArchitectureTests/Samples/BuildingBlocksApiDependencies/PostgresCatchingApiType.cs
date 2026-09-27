using Npgsql;

namespace EmergencyHub.ArchitectureTests.Samples.BuildingBlocksApiDependencies;

/// <summary>위반 예: 메서드 본문에서만 Npgsql 예외의 SqlState를 읽는다.</summary>
public sealed class PostgresCatchingApiType
{
    /// <summary>표본 메서드.</summary>
    /// <param name="exception">예외.</param>
    /// <returns>SqlState.</returns>
    public static string? SqlState(Exception exception) => (exception as PostgresException)?.SqlState;
}
