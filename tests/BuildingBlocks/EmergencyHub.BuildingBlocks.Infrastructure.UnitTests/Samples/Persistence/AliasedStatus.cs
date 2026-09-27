namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>엣지: 같은 값의 별칭(Enabled = Active)과 음수 값이 있는 코드 enum. 목록은 중복 없이 오름차순이어야 한다.</remarks>
public enum AliasedStatus : short
{
    Unknown = 0,
    Active = 1,
    Enabled = Active,
    Legacy = -1,
    Suspended = 2,
}
