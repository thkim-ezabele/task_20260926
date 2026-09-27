namespace EmergencyHub.ArchitectureTests.References;

/// <summary>
/// 선언 참조(csproj의 ProjectReference · PackageReference) 규칙. <b>쓰지 않는 참조도 막는다</b>(S02-T05 결정):
/// 참조만 있어도 금지 형식을 쓸 수 있는 상태가 되고, 전이 참조로 상위 프로젝트까지 번지기 때문이다.
/// </summary>
/// <remarks>
/// 원본: ADR-0024 의존성 규칙 표 "참조 금지" 열(Domain은 "참조 가능" 열의 "(없음)"). "참조 가능" 열을 허용 목록으로 쓰지 않는 이유:
/// Infrastructure 행은 UUIDNext(ADR-0013) · FluentValidation DI 확장(ADR-0018) · EFCore.NamingConventions처럼 다른 ADR이 승인한 참조를 다 적지 않았다.
/// 서비스 Api 행은 선언 참조 금지가 없다(Controller의 Infrastructure 사용 금지는 형식 규칙, S03).
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
        ArgumentNullException.ThrowIfNull(declared);

        if (project.Layer == ArchitectureLayer.Domain)
        {
            var domainNames = ArchitectureAssemblies.NamesIn(ArchitectureLayer.Domain);

            return [.. declared.Where(name => !domainNames.Contains(name, StringComparer.Ordinal))];
        }

        var forbidden = ForbiddenPrefixes(project);

        return [.. declared.Where(name => forbidden.Any(prefix => ForbiddenDependencies.Matches(name, prefix)))];
    }

    private static string[] ForbiddenPrefixes(LayerAssembly project) => project.Layer switch
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
        ArchitectureLayer.Api when project.IsBuildingBlocks => ForbiddenDependencies.Combine(
            ArchitectureAssemblies.NamesIn(ArchitectureLayer.Infrastructure),
            ForbiddenDependencies.EfCore,
            ForbiddenDependencies.Npgsql),
        ArchitectureLayer.Api => [],
        _ => throw new ArgumentOutOfRangeException(nameof(project), project.Layer, "레이어가 정해지지 않은 프로젝트다."),
    };
}
