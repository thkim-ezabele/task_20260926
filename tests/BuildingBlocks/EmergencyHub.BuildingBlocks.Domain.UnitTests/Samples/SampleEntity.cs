using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Samples;

public sealed class SampleEntity : Entity<SampleId>
{
    public SampleEntity(SampleId id)
        : base(id)
    {
    }

    // ORM 구체화 경로(Id 기본값)를 흉내 낸다.
    public SampleEntity()
    {
    }
}
