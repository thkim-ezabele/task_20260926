using EmergencyHub.BuildingBlocks.Domain.Entities;
using EmergencyHub.BuildingBlocks.Domain.Events;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Entities;

public sealed class AggregateRootTests
{
    [Fact]
    public void DomainEvents_WhenNothingRaised_IsEmpty()
    {
        var aggregate = new SampleAggregate(SampleId.New());

        var domainEvents = aggregate.DomainEvents;

        domainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raise_WithOneEvent_CollectsEvent()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        var domainEvent = new SampleDomainEvent(1);

        aggregate.Happen(domainEvent);

        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(domainEvent);
    }

    [Fact]
    public void Raise_WithMultipleEvents_CollectsInRaisedOrder()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        IDomainEvent[] domainEvents = [new SampleDomainEvent(1), new SampleDomainEvent(2), new SampleDomainEvent(3)];

        foreach (var domainEvent in domainEvents)
        {
            aggregate.Happen(domainEvent);
        }

        aggregate.DomainEvents.Should().Equal(domainEvents);
    }

    [Fact]
    public void Raise_WithSameEventTwice_CollectsBoth()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        var domainEvent = new SampleDomainEvent(1);
        aggregate.Happen(domainEvent);

        aggregate.Happen(domainEvent);

        aggregate.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void Raise_WithNull_ThrowsArgumentNullException()
    {
        var aggregate = new SampleAggregate(SampleId.New());

        var act = () => aggregate.Happen(null!);

        act.Should().Throw<ArgumentNullException>();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_AfterRaise_LeavesNoEvents()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        aggregate.Happen(new SampleDomainEvent(1));
        aggregate.Happen(new SampleDomainEvent(2));

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_CalledTwice_RemainsEmpty()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        aggregate.Happen(new SampleDomainEvent(1));
        aggregate.ClearDomainEvents();

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_WhenNothingRaised_RemainsEmpty()
    {
        var aggregate = new SampleAggregate(SampleId.New());

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raise_AfterClear_CollectsOnlyNewEvents()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        aggregate.Happen(new SampleDomainEvent(1));
        aggregate.ClearDomainEvents();
        var newEvent = new SampleDomainEvent(2);

        aggregate.Happen(newEvent);

        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(newEvent);
    }

    [Fact]
    public void DomainEvents_WhenCastToMutableCollection_ThrowsNotSupportedException()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        var collection = (ICollection<IDomainEvent>)aggregate.DomainEvents;

        var act = () => collection.Add(new SampleDomainEvent(1));

        act.Should().Throw<NotSupportedException>();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    // ---- IHasDomainEvents: UnitOfWork가 형식 인자 없이 커밋 뒤 이벤트를 비우는 계약(S02-T07) ----

    [Fact]
    public void AggregateRoot_Always_ImplementsNonGenericIHasDomainEvents()
    {
        typeof(AggregateRoot<SampleId>).Should().Implement<IHasDomainEvents>();
    }

    [Fact]
    public void DomainEvents_ThroughIHasDomainEvents_ReturnsSameEventsInRaisedOrder()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        IDomainEvent[] domainEvents = [new SampleDomainEvent(1), new SampleDomainEvent(2)];
        foreach (var domainEvent in domainEvents)
        {
            aggregate.Happen(domainEvent);
        }

        var events = ((IHasDomainEvents)aggregate).DomainEvents;

        events.Should().Equal(domainEvents);
    }

    [Fact]
    public void ClearDomainEvents_ThroughIHasDomainEvents_EmptiesAggregateEvents()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        aggregate.Happen(new SampleDomainEvent(1));

        ((IHasDomainEvents)aggregate).ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_ThroughIHasDomainEventsWhenNothingRaised_DoesNotThrow()
    {
        var aggregate = new SampleAggregate(SampleId.New());

        var act = ((IHasDomainEvents)aggregate).ClearDomainEvents;

        act.Should().NotThrow();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void IHasDomainEvents_PublicSurface_OnlyReadsAndClearsEventsWithoutRaise()
    {
        // 밖에서 이벤트를 넣을 수 없어야 한다(발생은 Aggregate 도메인 메서드만).
        typeof(IHasDomainEvents).GetMembers().Select(member => member.Name)
            .Should().BeEquivalentTo(["DomainEvents", "get_DomainEvents", "ClearDomainEvents"]);
    }

    [Fact]
    public void Constructor_WithId_SetsIdAndInheritsEntityEquality()
    {
        var id = SampleId.New();

        var aggregate = new SampleAggregate(id);

        aggregate.Id.Should().Be(id);
        aggregate.Should().Be(new SampleAggregate(id));
    }
}
