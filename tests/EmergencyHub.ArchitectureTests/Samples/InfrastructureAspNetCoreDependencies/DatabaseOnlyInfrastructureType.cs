using Npgsql;

namespace EmergencyHub.ArchitectureTests.Samples.InfrastructureAspNetCoreDependencies;

/// <summary>규칙을 지킨 예: Npgsql만 쓴다(Infrastructure 허용).</summary>
public sealed class DatabaseOnlyInfrastructureType
{
    /// <summary>연결.</summary>
    public NpgsqlConnection? Connection { get; init; }
}
