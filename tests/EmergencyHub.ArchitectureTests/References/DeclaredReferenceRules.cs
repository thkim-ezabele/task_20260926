namespace EmergencyHub.ArchitectureTests.References;

/// <summary>
/// 선언 참조(csproj의 ProjectReference · PackageReference) 규칙. <b>쓰지 않는 참조도 막는다</b>(S02-T05 결정):
/// 참조만 있어도 금지 형식을 쓸 수 있는 상태가 되고, 전이 참조로 상위 프로젝트까지 번지기 때문이다.
/// </summary>
/// <remarks>
/// 원본: ADR-0024 의존성 규칙 표 "참조 금지" 열(Domain은 "참조 가능" 열: BuildingBlocks.Domain은 "(없음)", <c>&lt;Service&gt;.Domain</c>은
/// BuildingBlocks.Domain만)과 표 아래 "서비스끼리는 프로젝트를 참조하지 않는다". "참조 가능" 열을 허용 목록으로 쓰지 않는 이유:
/// Infrastructure 행은 UUIDNext(ADR-0013) · FluentValidation DI 확장(ADR-0018) · EFCore.NamingConventions처럼 다른 ADR이 승인한 참조를 다 적지 않았다.
/// 서비스 Api 행은 레이어 금지가 없다(Controller의 Infrastructure 사용 금지는 형식 규칙 <c>ControllersDoNotUseInfrastructureOrRepositories</c>).
/// 서비스 격리는 레이어와 관계없이 모든 서비스 프로젝트에 적용한다: 제품 이름(<c>EmergencyHub.*</c>) 중 자기 서비스 · BuildingBlocks ·
/// ServiceDefaults가 아닌 것은 다른 서비스다(목록에 없는 서비스도 잡는다).
/// </remarks>
public static class DeclaredReferenceRules
{
    /// <summary>프로젝트의 선언 참조 중 규칙을 어긴 것을 찾는다.</summary>
    /// <param name="project">검사할 프로젝트.</param>
    /// <param name="declared">선언 참조 이름.</param>
    /// <returns>금지된 참조 이름. 없으면 빈 목록.</returns>
    public static IReadOnlyList<string> FindViolations(LayerAssembly project, IEnumerable<string> declared)
    {
        ArgumentNullException.ThrowIfNull(project);

        return FindViolations(project.Name, project.Layer, declared);
    }

    /// <summary>프로젝트 이름과 레이어로 선언 참조 중 규칙을 어긴 것을 찾는다(목록에 아직 없는 서비스 프로젝트 검사에도 쓴다).</summary>
    /// <param name="projectName">프로젝트 이름.</param>
    /// <param name="layer">프로젝트 레이어.</param>
    /// <param name="declared">선언 참조 이름.</param>
    /// <returns>금지된 참조 이름(선언 순서, 중복 없음). 없으면 빈 목록.</returns>
    public static IReadOnlyList<string> FindViolations(string projectName, ArchitectureLayer layer, IEnumerable<string> declared)
    {
        ArgumentNullException.ThrowIfNull(projectName);
        ArgumentNullException.ThrowIfNull(declared);

        var names = declared.ToList();
        var layerViolations = layer == ArchitectureLayer.Domain
            ? DomainViolations(names)
            : ForbiddenByLayer(projectName, layer, names);

        return [.. layerViolations.Concat(OtherServiceReferences(projectName, names)).Distinct(StringComparer.Ordinal)];
    }

    private static IEnumerable<string> DomainViolations(IEnumerable<string> declared)
    {
        var allowed = ArchitectureAssemblies.In(ArchitectureLayer.Domain).Where(assembly => assembly.IsBuildingBlocks).Select(assembly => assembly.Name).ToList();

        return declared.Where(name => !allowed.Contains(name, StringComparer.Ordinal));
    }

    private static IEnumerable<string> ForbiddenByLayer(string projectName, ArchitectureLayer layer, IEnumerable<string> declared)
    {
        var forbidden = ForbiddenPrefixes(projectName, layer);

        return declared.Where(name => forbidden.Any(prefix => ForbiddenDependencies.Matches(name, prefix)));
    }

    private static IEnumerable<string> OtherServiceReferences(string projectName, IEnumerable<string> declared)
    {
        var service = ProductNames.ServiceOf(projectName);

        if (service is null)
        {
            return [];
        }

        return declared.Where(name => ProductNames.ServiceOf(name) is { } referenced && !string.Equals(referenced, service, StringComparison.Ordinal));
    }

    private static string[] ForbiddenPrefixes(string projectName, ArchitectureLayer layer) => layer switch
    {
        ArchitectureLayer.Application => ForbiddenDependencies.Combine(
            ArchitectureAssemblies.NamesIn(ArchitectureLayer.Infrastructure),
            ArchitectureAssemblies.NamesIn(ArchitectureLayer.Api),
            ForbiddenDependencies.EfCore,
            ForbiddenDependencies.Npgsql,
            ForbiddenDependencies.Scrutor,
            ForbiddenDependencies.AspNetCore),
        ArchitectureLayer.Infrastructure => ForbiddenDependencies.Combine(
            ArchitectureAssemblies.NamesIn(ArchitectureLayer.Api),
            ForbiddenDependencies.AspNetCore),
        ArchitectureLayer.Api when ProductNames.OwnershipOf(projectName) == AssemblyOwnership.BuildingBlocks => ForbiddenDependencies.Combine(
            ArchitectureAssemblies.NamesIn(ArchitectureLayer.Infrastructure),
            ForbiddenDependencies.EfCore,
            ForbiddenDependencies.Npgsql),
        ArchitectureLayer.Api => [],
        ArchitectureLayer.MigrationService => ForbiddenDependencies.Combine(ArchitectureAssemblies.NamesIn(ArchitectureLayer.Api)),
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "레이어가 정해지지 않은 프로젝트다."),
    };
}
