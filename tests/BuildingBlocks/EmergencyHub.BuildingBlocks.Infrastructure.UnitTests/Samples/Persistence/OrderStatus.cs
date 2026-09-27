namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>정의 순서를 일부러 섞어 체크 제약 목록이 값 오름차순인지 확인한다.</remarks>
public enum OrderStatus : short
{
    Unknown = 0,
    Shipped = 2,
    Placed = 1,
    Cancelled = 3,
}
