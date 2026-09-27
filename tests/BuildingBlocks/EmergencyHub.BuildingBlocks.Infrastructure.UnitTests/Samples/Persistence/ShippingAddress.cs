namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>OwnsOne(테이블 분할) + 중첩 owned(Location) 확인용 Value Object.</remarks>
public sealed record ShippingAddress(string City, string ZipCode)
{
    public GeoPoint Location { get; init; } = new(0m, 0m);
}
