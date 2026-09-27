using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.ArchitectureTests.Samples.EntityIds;

/// <summary>규칙을 지킨 예: 강타입 ID 키.</summary>
public sealed class StronglyKeyedEntity : Entity<SampleId>
{
    /// <summary>표본 생성자.</summary>
    /// <param name="id">ID.</param>
    public StronglyKeyedEntity(SampleId id)
        : base(id)
    {
    }
}
