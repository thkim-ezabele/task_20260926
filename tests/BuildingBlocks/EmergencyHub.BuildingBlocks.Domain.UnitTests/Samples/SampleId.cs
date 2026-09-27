using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Samples;

public readonly record struct SampleId(Guid Value) : IStronglyTypedId<SampleId>
{
    public static SampleId New() => new(Guid.NewGuid());
}
