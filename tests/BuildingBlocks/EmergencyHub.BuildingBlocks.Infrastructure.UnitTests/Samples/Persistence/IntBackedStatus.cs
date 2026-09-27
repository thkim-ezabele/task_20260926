namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>규칙 위반 예시: 코드값인데 기반 형식이 short가 아니다.</remarks>
public enum IntBackedStatus : int
{
    Unknown = 0,
    Active = 1,
}
