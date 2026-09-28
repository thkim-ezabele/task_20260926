using EmergencyHub.BuildingBlocks.Application.Identifiers;

namespace EmergencyHub.ArchitectureTests.Samples.ImplementationVisibility;

/// <summary>위반 예: public이고 sealed도 아닌 포트 구현.</summary>
public class OpenPublicIdGenerator : IIdGenerator
{
    /// <inheritdoc />
    public Guid NewId() => Guid.Empty;
}
