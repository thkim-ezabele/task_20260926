namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Samples;

public readonly record struct SampleId(Guid Value)
{
    public static SampleId New() => new(Guid.NewGuid());
}
