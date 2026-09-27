using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>서비스 Infrastructure가 소유하는 ux_ 이름 상수 한 곳(매핑과 23505 매핑 레지스트리가 함께 참조, database.md).</remarks>
public static class SampleIndexNames
{
    public static readonly UniqueIndexName OrdersOrderNumber = new("ux_orders_order_number");
}
