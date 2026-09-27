using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.ArchitectureTests.Samples.EntitySealing;

/// <summary>규칙을 지킨 예: sealed Entity.</summary>
public sealed class SealedSampleEntity : Entity<SampleId>
{
    /// <summary>표본 생성자.</summary>
    /// <param name="id">ID.</param>
    public SealedSampleEntity(SampleId id)
        : base(id)
    {
    }
}
