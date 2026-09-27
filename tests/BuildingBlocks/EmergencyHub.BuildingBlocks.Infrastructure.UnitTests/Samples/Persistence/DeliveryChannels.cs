namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>비트 자리에 빈 곳(4)이 있어 마스크(11)와 범위 조건(&lt;= 11)이 다르다. 조합 별칭 All은 마스크에 영향이 없다.</remarks>
[Flags]
public enum DeliveryChannels : int
{
    None = 0,
    Sms = 1 << 0,
    Push = 1 << 1,
    Email = 1 << 3,

    All = Sms | Push | Email,
}
