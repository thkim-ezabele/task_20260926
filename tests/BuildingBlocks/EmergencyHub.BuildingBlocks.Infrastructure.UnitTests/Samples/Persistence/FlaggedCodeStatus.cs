namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>규칙 위반 예시: short지만 [Flags]라 코드값 도우미 대상이 아니다.</remarks>
[Flags]
public enum FlaggedCodeStatus : short
{
    None = 0,
    First = 1 << 0,
}
