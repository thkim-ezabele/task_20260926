namespace EmergencyHub.ArchitectureTests.Samples.EnumTypes;

/// <summary>규칙을 지킨 예: [Flags] long.</summary>
[Flags]
public enum SampleWidePermissions : long
{
    /// <summary>없음.</summary>
    None = 0,

    /// <summary>읽기.</summary>
    Read = 1L << 40,
}
