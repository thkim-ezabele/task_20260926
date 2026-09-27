namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>부호 비트(1 &lt;&lt; 63)를 쓰는 비트 플래그 예시. 0 멤버가 없습니다.</summary>
[Flags]
public enum WideSampleChannels : long
{
    First = 1L << 0,
    Last = 1L << 63,
}
