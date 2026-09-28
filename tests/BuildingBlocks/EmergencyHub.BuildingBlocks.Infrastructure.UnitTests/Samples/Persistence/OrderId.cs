using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

public readonly record struct OrderId(Guid Value) : IStronglyTypedId<OrderId>
{
    public static OrderId New() => new(Guid.NewGuid());
}
