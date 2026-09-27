namespace EmergencyHub.ArchitectureTests.Samples.DomainDependencies;

/// <summary>규칙을 지킨 예: System(BCL)만 쓴다.</summary>
public sealed class SystemOnlyDomainType
{
    /// <summary>표본 메서드.</summary>
    /// <param name="at">시각.</param>
    /// <returns>키.</returns>
    public static string Key(DateTimeOffset at) => string.Concat(Guid.Empty.ToString("N"), at.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
}
