namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>비트 플래그 예시(OpenAPI 정수 enum 설명 확인용).</summary>
[Flags]
public enum SampleChannels : int
{
    None = 0,
    Sms = 1 << 0,
    Push = 1 << 1,
    Email = 1 << 2,

    All = Sms | Push | Email,
}
