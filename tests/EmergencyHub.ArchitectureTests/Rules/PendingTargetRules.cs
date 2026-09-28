namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 대상 대기 목록입니다. 원본: testing-strategy "대상 대기 목록" 표(S05-T02), 구현 S05-T04.
/// </summary>
/// <remarks>
/// <para>
/// PRD-001 샘플 API · Validator를 지워(S05-T04) 제품 대상이 0개가 된 서비스 전용 규칙 3개만 둡니다. 이 규칙들은 대상 0개일 때
/// "공허 통과 실패" 대신 해제 작업 ID를 적은 건너뜀으로 표시합니다(<see cref="RuleCheck.ShouldPassOnProduct"/>).
/// </para>
/// <para>
/// 안전장치: 목록에 있는 규칙에 제품 대상이 1개 이상 생기면 실패합니다("목록에서 빼라"). 해제 작업은 여기서 항목을 지워
/// 대상 1개 이상 단언으로 되돌립니다. 표본 테스트(위반 예시만 정확히 잡는지)는 대기 중에도 그대로 돕니다.
/// </para>
/// </remarks>
public static class PendingTargetRules
{
    /// <summary>대기 중인 규칙과 해제 작업 ID.</summary>
    public static IReadOnlyList<PendingTargetRule> All { get; } =
    [
        new(ConventionRules.ValidatorsDeriveFromRequestValidator, "S06-T04"),
        new(InjectionRules.ValidatorsDoNotInjectRepositoriesOrServices, "S06-T04"),
        new(DependencyRules.ControllersDoNotUseInfrastructureOrRepositories, "S06-T05"),
    ];

    /// <summary>규칙이 대기 목록에 있으면 그 항목을, 없으면 <see langword="null"/>을 돌려줍니다(같은 인스턴스 기준).</summary>
    /// <param name="rule">찾을 규칙.</param>
    /// <returns>대기 항목 또는 <see langword="null"/>.</returns>
    public static PendingTargetRule? Find(ArchitectureRule rule) =>
        All.SingleOrDefault(pending => ReferenceEquals(pending.Rule, rule));
}
