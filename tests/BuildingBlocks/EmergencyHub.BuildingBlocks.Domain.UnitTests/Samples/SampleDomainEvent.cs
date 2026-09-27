using EmergencyHub.BuildingBlocks.Domain.Events;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Samples;

public sealed record SampleDomainEvent(int Sequence) : IDomainEvent;
