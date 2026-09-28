using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.ArchitectureTests.Samples.EntityIds;

/// <summary>위반 예: Guid를 그대로 키로 쓴다.</summary>
public sealed class GuidKeyedEntity : Entity<Guid>
{
    /// <summary>표본 생성자.</summary>
    /// <param name="id">ID.</param>
    public GuidKeyedEntity(Guid id)
        : base(id)
    {
    }
}
