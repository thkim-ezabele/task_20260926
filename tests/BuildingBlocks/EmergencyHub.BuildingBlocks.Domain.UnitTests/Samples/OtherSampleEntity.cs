using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Samples;

public sealed class OtherSampleEntity : Entity<SampleId>
{
    public OtherSampleEntity(SampleId id)
        : base(id)
    {
    }
}
