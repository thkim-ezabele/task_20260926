using EmergencyHub.ArchitectureTests.Samples.ServiceIsolationOrdering;

namespace EmergencyHub.ArchitectureTests.Samples.ServiceIsolation;

/// <summary>위반 예: 다른 서비스 형식을 속성으로 쓴다.</summary>
public sealed class OtherServicePropertyType
{
    /// <summary>다른 서비스의 주문.</summary>
    public OrderSnapshot? Order { get; init; }
}
