namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>규칙 하나를 범위 하나에 적용한 결과.</summary>
/// <param name="Rule">적용한 규칙.</param>
/// <param name="TargetNames">대상 형식 이름(Mono.Cecil 표기: 중첩은 <c>/</c>, 제네릭은 <c>`N</c>).</param>
/// <param name="IsSuccessful">모든 대상이 조건을 지켰으면 <see langword="true"/>.</param>
/// <param name="FailingTypeNames">조건을 어긴 형식 이름.</param>
public sealed record RuleCheck(
    ArchitectureRule Rule,
    IReadOnlyList<string> TargetNames,
    bool IsSuccessful,
    IReadOnlyList<string> FailingTypeNames)
{
    /// <summary>
    /// 제품 어셈블리가 규칙을 지키는지 단언한다. 대상이 0개면 공허 통과이므로 실패한다
    /// (서비스 전용 규칙은 서비스 어셈블리가 목록에 없는 동안만 건너뜀).
    /// </summary>
    /// <remarks>
    /// 대상 대기 목록(<see cref="PendingTargetRules"/>)의 규칙은 대상이 0개면 해제 작업 ID를 적어 건너뛰고,
    /// 대상이 1개 이상이면 "목록에서 빼라"로 실패한다(안전장치, S05-T04).
    /// </remarks>
    public void ShouldPassOnProduct()
    {
        if (PendingTargetRules.Find(Rule) is { } pending)
        {
            TargetNames.Should().BeEmpty(
                "{0}: 대상 대기 목록의 규칙에 제품 대상이 생겼다. PendingTargetRules에서 이 규칙을 목록에서 빼라(해제 작업 {1}, 원본: testing-strategy 대상 대기 목록)",
                Rule.Name,
                pending.ReleaseTaskId);
            Assert.Skip($"{Rule.Name}: 대상 대기(해제 작업 {pending.ReleaseTaskId}). PRD-001 샘플 제거(S05-T04)로 제품 대상이 0개다. 규칙 동작은 표본 테스트가 확인한다.");
        }

        if (TargetNames.Count == 0)
        {
            Assert.SkipWhen(
                Rule.TargetsOnlyInServices && !ArchitectureAssemblies.HasServiceAssemblies,
                $"{Rule.Name}: 대상이 서비스 코드에만 있는 규칙이다. 서비스 어셈블리를 ArchitectureAssemblies에 추가하면 적용된다(S03). 규칙 동작은 표본 테스트가 확인한다.");
        }

        TargetNames.Should().NotBeEmpty("{0}: 대상 형식이 0개면 공허 통과다(원본: {1})", Rule.Name, Rule.Source);
        FailingTypeNames.Should().BeEmpty("{0}을(를) 어긴 형식이 없어야 한다(원본: {1})", Rule.Name, Rule.Source);
        IsSuccessful.Should().BeTrue();
    }

    /// <summary>
    /// 표본 범위에서 규칙이 <paramref name="violators"/>만 정확히 잡는지 단언한다. 표본 범위에는 규칙을 지킨 형식이
    /// 하나 이상 있어야 한다(오탐이 없음을 함께 확인).
    /// </summary>
    /// <param name="violators">규칙을 일부러 어긴 표본 형식.</param>
    public void ShouldFlagExactly(params Type[] violators)
    {
        ArgumentNullException.ThrowIfNull(violators);

        var expected = violators.Select(CecilName).ToList();

        expected.Should().NotBeEmpty();
        TargetNames.Should().Contain(expected, "{0}: 위반 예시가 규칙 대상으로 선택돼야 한다", Rule.Name);
        TargetNames.Except(expected).Should().NotBeEmpty("{0}: 표본에 규칙을 지킨 대상이 있어야 오탐 여부를 확인할 수 있다", Rule.Name);
        IsSuccessful.Should().BeFalse("{0}: 위반 예시가 있으면 규칙이 실패해야 한다", Rule.Name);
        FailingTypeNames.Should().BeEquivalentTo(expected, "{0}: 위반 예시만 정확히 잡아야 한다", Rule.Name);
    }

    private static string CecilName(Type type) => type.FullName!.Replace('+', '/');
}
