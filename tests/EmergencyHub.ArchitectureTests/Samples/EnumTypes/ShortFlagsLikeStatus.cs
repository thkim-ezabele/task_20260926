namespace EmergencyHub.ArchitectureTests.Samples.EnumTypes;

/// <summary>위반 예: 일반 코드에 long 기반.</summary>
public enum ShortFlagsLikeStatus : long
{
    /// <summary>예약.</summary>
    Unknown = 0,

    /// <summary>활성.</summary>
    Active = 1,
}
