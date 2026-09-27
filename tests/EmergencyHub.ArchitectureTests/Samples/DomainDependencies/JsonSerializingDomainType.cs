using System.Text.Json;

namespace EmergencyHub.ArchitectureTests.Samples.DomainDependencies;

/// <summary>위반 예: 메서드 본문에서만 직렬화 라이브러리(System.Text.Json)를 쓴다.</summary>
public sealed class JsonSerializingDomainType
{
    /// <summary>표본 메서드.</summary>
    /// <param name="value">값.</param>
    /// <returns>JSON.</returns>
    public static string Serialize(int value) => JsonSerializer.Serialize(value);
}
