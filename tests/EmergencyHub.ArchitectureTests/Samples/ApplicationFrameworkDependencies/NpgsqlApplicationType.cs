using Npgsql;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationFrameworkDependencies;

/// <summary>위반 예: Npgsql 형식을 쓴다.</summary>
public sealed class NpgsqlApplicationType
{
    /// <summary>연결.</summary>
    public NpgsqlConnection? Connection { get; init; }
}
