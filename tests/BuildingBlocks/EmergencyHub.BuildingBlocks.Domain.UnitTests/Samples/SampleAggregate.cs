using EmergencyHub.BuildingBlocks.Domain.Entities;
using EmergencyHub.BuildingBlocks.Domain.Events;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Samples;

public sealed class SampleAggregate : AggregateRoot<SampleId>
{
    public SampleAggregate(SampleId id)
        : base(id)
    {
    }

    public void Happen(IDomainEvent domainEvent) => Raise(domainEvent);
}
