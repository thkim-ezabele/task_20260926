namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>코드 enum · [Flags] enum을 가진 owned Value Object(테이블 분할). ck_ 이름이 소유자 테이블 이름으로 만들어지는지 확인한다.</remarks>
public sealed record DispatchRoute(OrderStatus RouteStatus, DeliveryChannels RouteChannels);
