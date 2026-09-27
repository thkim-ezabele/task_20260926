namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>엣지: 32번째 비트 이상을 쓰는 long [Flags](bigint 컬럼).</remarks>
[Flags]
public enum WideChannels : long
{
    None = 0,
    First = 1L << 0,
    Far = 1L << 40,
    Last = 1L << 62,
}
