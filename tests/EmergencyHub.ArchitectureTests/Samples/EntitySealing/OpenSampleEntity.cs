using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.ArchitectureTests.Samples.EntitySealing;

/// <summary>위반 예: sealed가 아닌 Entity.</summary>
public class OpenSampleEntity : Entity<SampleId>
{
    /// <summary>표본 생성자.</summary>
    /// <param name="id">ID.</param>
    public OpenSampleEntity(SampleId id)
        : base(id)
    {
    }
}
