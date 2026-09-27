using EmergencyHub.BuildingBlocks.Domain.Events;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

public sealed record OrderPlacedDomainEvent(OrderId OrderId) : IDomainEvent;
