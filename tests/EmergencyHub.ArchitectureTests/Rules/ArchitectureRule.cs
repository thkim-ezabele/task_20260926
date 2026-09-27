namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 아키텍처 규칙 하나. 제품 범위와 표본 범위에 <b>같은 대상 선택 · 조건</b>을 적용해, 제품 통과와 위반 예시 실패를 같은 코드로 확인한다.
/// </summary>
/// <param name="Name">규칙 이름(실패 메시지에 쓴다).</param>
/// <param name="Source">규칙 원본(ADR 표 행 · 기준 문서 절).</param>
/// <param name="ProductScope">제품 어셈블리 범위.</param>
/// <param name="SelectTargets">범위 안에서 규칙 대상을 고른다. 레이어 의존 규칙은 범위 전체가 대상이다.</param>
/// <param name="Condition">대상이 지켜야 할 조건.</param>
/// <param name="TargetsOnlyInServices">
/// 대상이 서비스 코드에만 있는 규칙(예: Command는 record)이면 <see langword="true"/>. 서비스 어셈블리가 목록에 없는 동안(S03 전)만
/// 대상 0개를 건너뜀(Skip)으로 표시하고, 서비스 어셈블리가 들어오면 대상 0개는 실패다.
/// </param>
public sealed record ArchitectureRule(
    string Name,
    string Source,
    Func<PredicateList> ProductScope,
    Func<PredicateList, PredicateList> SelectTargets,
    Func<Conditions, ConditionList> Condition,
    bool TargetsOnlyInServices = false)
{
    /// <summary>범위 전체를 대상으로 삼는다(레이어 의존 규칙).</summary>
    /// <param name="scope">범위.</param>
    /// <returns>범위 그대로.</returns>
    public static PredicateList AllTypes(PredicateList scope) => scope;

    /// <summary>제품 어셈블리에 규칙을 적용한다.</summary>
    /// <returns>검사 결과.</returns>
    public RuleCheck CheckProduct() => Check(ProductScope);

    /// <summary>주어진 범위에 규칙을 적용한다.</summary>
    /// <param name="scope">범위 생성 함수(호출마다 새 술어 목록).</param>
    /// <returns>검사 결과.</returns>
    public RuleCheck Check(Func<PredicateList> scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var targets = SelectTargets(scope()).GetTypes().Select(type => type.FullName!.Replace('+', '/')).ToList();
        var result = Condition(SelectTargets(scope()).Should()).GetResult();

        return new RuleCheck(this, targets, result.IsSuccessful, [.. result.FailingTypeNames ?? []]);
    }
}
