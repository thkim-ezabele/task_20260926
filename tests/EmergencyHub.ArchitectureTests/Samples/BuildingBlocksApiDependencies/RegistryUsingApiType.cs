using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;

namespace EmergencyHub.ArchitectureTests.Samples.BuildingBlocksApiDependencies;

/// <summary>위반 예: BuildingBlocks.Infrastructure 형식을 쓴다.</summary>
public sealed class RegistryUsingApiType
{
    /// <summary>23505 레지스트리.</summary>
    public UniqueConstraintErrorRegistry? Registry { get; init; }
}
