namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// 값이 없는 응답을 나타냅니다. 반환 값이 없는 Command(<see cref="ICommand"/>)는 <c>Result&lt;Unit&gt;</c>을 돌려줍니다.
/// </summary>
/// <remarks>
/// 반환 값이 있는 Command와 없는 Command를 같은 제네릭 인터페이스 · 같은 데코레이터 하나로 처리하려고 둡니다(ADR-0015).
/// 상태가 없으므로 모든 값이 서로 같습니다.
/// </remarks>
public readonly record struct Unit
{
    /// <summary>유일한 값입니다.</summary>
    public static Unit Value => default;
}
