namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 서비스마다 따로 만드는 규칙 묶음(예: 서비스 격리. 금지 대상이 "자기 말고 다른 서비스"라서 서비스마다 조건이 다르다).
/// </summary>
/// <param name="Name">규칙 이름(건너뜀 메시지에 쓴다).</param>
/// <param name="Source">규칙 원본(ADR 표 행 · 기준 문서 절).</param>
/// <param name="MinimumServiceCount">
/// 규칙이 의미를 갖는 최소 서비스 수. 목록의 서비스가 이보다 적으면 제품 검사를 건너뜀(Skip)으로 표시한다
/// (서비스 격리는 2: 서비스가 0 · 1개면 금지할 다른 서비스가 없어 공허 통과다).
/// </param>
/// <param name="ForService">서비스 접두사와 다른 서비스 접두사 목록으로 그 서비스의 규칙을 만든다.</param>
public sealed record ServiceRuleSet(
    string Name,
    string Source,
    int MinimumServiceCount,
    Func<string, IReadOnlyList<string>, ArchitectureRule> ForService)
{
    /// <summary>서비스마다 규칙 하나를 만든다(다른 서비스 = 목록에서 자기를 뺀 나머지).</summary>
    /// <param name="services">서비스 접두사 목록.</param>
    /// <returns>서비스 순서를 유지한 규칙.</returns>
    public IReadOnlyList<ArchitectureRule> ForServices(IReadOnlyList<string> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return [.. services.Select(service => ForService(service, [.. services.Where(other => !string.Equals(other, service, StringComparison.Ordinal))]))];
    }

    /// <summary>
    /// 목록(<see cref="ArchitectureAssemblies.ServiceNames"/>)의 서비스마다 규칙을 제품에 적용한다. 서비스가
    /// <see cref="MinimumServiceCount"/>개 미만이면 건너뛰고, 그 이상이면 서비스마다 공허 통과 방지 단언을 한다.
    /// </summary>
    public void ShouldPassOnProduct()
    {
        var services = ArchitectureAssemblies.ServiceNames;

        Assert.SkipWhen(
            services.Count < MinimumServiceCount,
            $"{Name}: 서비스가 {MinimumServiceCount}개 이상일 때 적용된다(현재 {services.Count}개). 규칙 동작은 표본 테스트가 확인하고, 선언 참조는 DeclaredReferenceTests가 서비스 수와 관계없이 막는다.");

        foreach (var rule in ForServices(services))
        {
            rule.CheckProduct().ShouldPassOnProduct();
        }
    }
}
