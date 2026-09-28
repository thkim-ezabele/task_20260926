namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>규칙 위반 예시: int지만 [Flags]가 없어 비트 플래그 도우미 대상이 아니다.</remarks>
public enum PlainIntCode : int
{
    None = 0,
    First = 1,
}
