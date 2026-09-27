using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// 공통 모델 규칙 확인용 Aggregate: 강타입 ID 키, 코드 enum(NULL 허용 포함), [Flags] enum, 유니크 인덱스, 일반 인덱스,
/// OwnsOne(중첩 owned 포함), OwnsMany, 도메인 이벤트.
/// PlacedEvent는 구체 도메인 이벤트 형식의 설정 가능한 속성이다. EF 8은 인터페이스 컬렉션(DomainEvents)을 원래 매핑하지 않으므로(실측),
/// IgnoreAny&lt;IDomainEvent&gt; 규칙이 없으면 EF가 엔티티로 발견해 모델 생성이 실패하는 경우를 만들어 규칙을 판별한다.
/// </remarks>
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderNote> _notes = [];

    private Order(OrderId id, string orderNumber, CustomerId? customerId, ShippingAddress shipping)
        : base(id)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        Shipping = shipping;
        OrderStatus = OrderStatus.Placed;
        DeliveryChannels = DeliveryChannels.Sms;
    }

    // EF Core 구체화용.
    private Order()
    {
    }

    public string OrderNumber { get; private set; } = string.Empty;

    public OrderStatus OrderStatus { get; private set; }

    public OrderStatus? PreviousOrderStatus { get; private set; }

    public DeliveryChannels DeliveryChannels { get; private set; }

    public CustomerId? CustomerId { get; private set; }

    public ShippingAddress Shipping { get; private set; } = null!;

    public IReadOnlyCollection<OrderNote> Notes => _notes.AsReadOnly();

    public OrderPlacedDomainEvent? PlacedEvent { get; private set; }

    public static Order Place(OrderId id, string orderNumber, CustomerId? customerId, ShippingAddress shipping)
    {
        var order = new Order(id, orderNumber, customerId, shipping);
        order.PlacedEvent = new OrderPlacedDomainEvent(id);
        order.Raise(order.PlacedEvent);
        return order;
    }

    public void Ship()
    {
        PreviousOrderStatus = OrderStatus;
        OrderStatus = OrderStatus.Shipped;
    }

    public void ChangeShipping(ShippingAddress shipping) => Shipping = shipping;

    public void AddNote(string text) => _notes.Add(new OrderNote(text));

    public void ClearNotes() => _notes.Clear();
}
