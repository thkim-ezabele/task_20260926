using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// 매핑이 테이블 이름(ToTable) · 컬럼 이름(HasColumnName)을 덮어쓰는 Aggregate. ck_ 도우미가 클래스 · 속성 이름이 아니라
/// 최종 메타데이터 이름을 쓰는지, long [Flags]가 bigint로 매핑되는지, 다른 Aggregate 참조 ID(OrderId)가 uuid로 변환되는지 확인한다.
/// </remarks>
public sealed class Dispatch : AggregateRoot<DispatchId>
{
    private Dispatch(DispatchId id, OrderId sourceOrderId, DispatchRoute route)
        : base(id)
    {
        SourceOrderId = sourceOrderId;
        Route = route;
        State = OrderStatus.Placed;
        Channels = WideChannels.First;
    }

    // EF Core 구체화용.
    private Dispatch()
    {
    }

    public OrderStatus State { get; private set; }

    public WideChannels Channels { get; private set; }

    public OrderId SourceOrderId { get; private set; }

    public DispatchRoute Route { get; private set; } = null!;

    public static Dispatch Create(DispatchId id, OrderId sourceOrderId, DispatchRoute route) => new(id, sourceOrderId, route);
}
