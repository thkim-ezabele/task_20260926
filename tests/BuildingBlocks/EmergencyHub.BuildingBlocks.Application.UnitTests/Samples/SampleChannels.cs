namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>비트 플래그 예시. 8(1 &lt;&lt; 3)은 정의되지 않은 비트입니다.</summary>
[Flags]
public enum SampleChannels : int
{
    None = 0,
    Sms = 1 << 0,
    Push = 1 << 1,
    Email = 1 << 2,

    All = Sms | Push | Email,
}
