using EmergencyHub.ArchitectureTests.Samples.ServiceIsolationOrdering;

namespace EmergencyHub.ArchitectureTests.Samples.ServiceIsolation;

/// <summary>위반 예: 다른 서비스 형식을 메서드 본문에서만 만든다(시그니처에는 드러나지 않음).</summary>
public sealed class OtherServiceMethodBodyType
{
    /// <summary>다른 서비스 형식으로 설명을 만든다.</summary>
    /// <returns>설명.</returns>
    public string Describe() => new OrderSnapshot("sample").Number;
}
