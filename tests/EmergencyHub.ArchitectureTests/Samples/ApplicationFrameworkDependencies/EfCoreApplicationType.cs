using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationFrameworkDependencies;

/// <summary>위반 예: EF Core 형식을 쓴다.</summary>
public sealed class EfCoreApplicationType
{
    /// <summary>DbContext.</summary>
    public DbContext? Db { get; init; }
}
