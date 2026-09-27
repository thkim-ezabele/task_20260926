namespace EmergencyHub.ArchitectureTests.Samples.EnumTypes;

/// <summary>위반 예: [Flags]에 byte 기반.</summary>
[Flags]
public enum ByteChannels : byte
{
    /// <summary>없음.</summary>
    None = 0,

    /// <summary>SMS.</summary>
    Sms = 1 << 0,
}
