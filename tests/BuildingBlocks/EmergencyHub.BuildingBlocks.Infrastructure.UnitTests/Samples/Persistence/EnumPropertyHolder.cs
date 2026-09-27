namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>체크 제약 도우미를 모델 빌더로 직접 호출해 보기 위한 속성 모음(DbContext에 넣지 않음).</remarks>
public sealed class EnumPropertyHolder
{
    public int Id { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public OrderStatus? NullableOrderStatus { get; set; }

    public DeliveryChannels DeliveryChannels { get; set; }

    public DeliveryChannels? NullableDeliveryChannels { get; set; }

    public IntBackedStatus IntBackedStatus { get; set; }

    public ShortBackedChannels ShortBackedChannels { get; set; }

    public UnknownOnlyStatus UnknownOnlyStatus { get; set; }

    public FlaggedCodeStatus FlaggedCodeStatus { get; set; }

    public PlainIntCode PlainIntCode { get; set; }

    public SignBitChannels SignBitChannels { get; set; }

    public NoBitChannels NoBitChannels { get; set; }

    public WideChannels WideChannels { get; set; }
}
