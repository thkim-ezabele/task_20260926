namespace EmergencyHub.ArchitectureTests.Samples.EnumTypes;

/// <summary>위반 예: 기반 형식을 빠뜨린 일반 코드(int가 됨).</summary>
public enum ImplicitIntStatus
{
    /// <summary>예약.</summary>
    Unknown = 0,

    /// <summary>활성.</summary>
    Active = 1,
}
