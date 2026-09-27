namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>부호 있는 값과 64비트 값을 쓰는 enum 예시(설명 형식 확인용).</summary>
public enum SignedSampleCode : long
{
    Negative = -1,
    Unknown = 0,
    Large = 1L << 40,
}
