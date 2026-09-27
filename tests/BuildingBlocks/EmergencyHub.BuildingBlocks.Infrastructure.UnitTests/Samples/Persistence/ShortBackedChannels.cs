namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>규칙 위반 예시: [Flags]인데 기반 형식이 int / long이 아니다.</remarks>
[Flags]
public enum ShortBackedChannels : short
{
    None = 0,
    First = 1 << 0,
}
