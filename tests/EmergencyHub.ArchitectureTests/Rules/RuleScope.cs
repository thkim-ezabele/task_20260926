using System.Text.RegularExpressions;

namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 규칙을 적용할 형식 범위를 만든다. NetArchTest의 술어 목록은 호출할 때마다 상태가 쌓이므로 범위는 매번 새로 만든다.
/// </summary>
public static class RuleScope
{
    private const string ProductNamespacePrefix = "EmergencyHub";

    /// <summary>검사 대상 제품 어셈블리 전체(<see cref="ArchitectureAssemblies.All"/>).</summary>
    /// <returns>범위 생성 함수.</returns>
    public static Func<PredicateList> Product() => () => ProductTypes(ArchitectureAssemblies.All);

    /// <summary>레이어 하나의 제품 어셈블리.</summary>
    /// <param name="layer">레이어.</param>
    /// <param name="ownership">
    /// 지정하면 그 소유의 어셈블리만(BuildingBlocks: ADR-0024 BuildingBlocks 고유 행, Service: 서비스 행). <see langword="null"/>이면 모두.
    /// </param>
    /// <returns>범위 생성 함수.</returns>
    public static Func<PredicateList> Layer(ArchitectureLayer layer, AssemblyOwnership? ownership = null) =>
        () => ProductTypes([.. ArchitectureAssemblies.In(layer).Where(assembly => ownership is null || assembly.Ownership == ownership)]);

    /// <summary>서비스 하나의 제품 어셈블리 전체(모든 레이어).</summary>
    /// <param name="service">서비스 접두사(<c>EmergencyHub.&lt;Service&gt;</c>).</param>
    /// <returns>범위 생성 함수.</returns>
    public static Func<PredicateList> Service(string service) =>
        () => ProductTypes([.. ArchitectureAssemblies.All.Where(assembly => string.Equals(assembly.ServiceName, service, StringComparison.Ordinal))]);

    /// <summary>
    /// 이 테스트 어셈블리 안 표본 네임스페이스 하나(<typeparamref name="TSample"/>의 네임스페이스와 정확히 같은 것만).
    /// 규칙마다 위반 예시와 규칙을 지킨 예시를 한 네임스페이스에 둔다.
    /// </summary>
    /// <typeparam name="TSample">표본 네임스페이스에 있는 형식 하나.</typeparam>
    /// <returns>범위 생성 함수.</returns>
    public static Func<PredicateList> Samples<TSample>()
    {
        var pattern = $"^{Regex.Escape(typeof(TSample).Namespace!)}$";

        return () => Types.InAssembly(typeof(TSample).Assembly).That().ResideInNamespaceMatching(pattern);
    }

    // 컴파일러가 루트 네임스페이스 밖에 만드는 형식(<PrivateImplementationDetails>, <>z__ReadOnlyList`1 등)은 제외한다(S02-T05 스파이크 실측).
    private static PredicateList ProductTypes(IReadOnlyList<LayerAssembly> assemblies) =>
        Types.InAssemblies(assemblies.Select(assembly => assembly.Assembly)).That().ResideInNamespaceStartingWith(ProductNamespacePrefix);
}
