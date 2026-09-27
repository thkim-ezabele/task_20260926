namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>엣지: int 부호 비트(1 &lt;&lt; 31)를 쓴 [Flags]. 저장 값이 음수가 되어 col &gt;= 0 조건과 충돌한다.</remarks>
[Flags]
public enum SignBitChannels
{
    None = 0,
    First = 1 << 0,
    SignBit = 1 << 31,
}
