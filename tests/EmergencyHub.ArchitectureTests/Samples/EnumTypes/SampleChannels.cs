namespace EmergencyHub.ArchitectureTests.Samples.EnumTypes;

/// <summary>규칙을 지킨 예: [Flags] int.</summary>
[Flags]
public enum SampleChannels : int
{
    /// <summary>없음.</summary>
    None = 0,

    /// <summary>SMS.</summary>
    Sms = 1 << 0,
}
