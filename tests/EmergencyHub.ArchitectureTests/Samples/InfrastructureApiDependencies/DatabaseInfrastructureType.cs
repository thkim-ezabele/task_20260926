using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.ArchitectureTests.Samples.InfrastructureApiDependencies;

/// <summary>규칙을 지킨 예: EF Core만 쓴다(Infrastructure 허용).</summary>
public sealed class DatabaseInfrastructureType
{
    /// <summary>DbContext.</summary>
    public DbContext? Db { get; init; }
}
