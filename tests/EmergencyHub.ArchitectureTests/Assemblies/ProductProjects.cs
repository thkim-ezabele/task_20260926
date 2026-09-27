namespace EmergencyHub.ArchitectureTests.Assemblies;

/// <summary>
/// 저장소 <c>src</c> 트리의 제품 프로젝트 목록. <see cref="ArchitectureAssemblies.All"/>에 빠진 프로젝트를 찾는 안전장치에 쓴다(BL-085).
/// </summary>
public static class ProductProjects
{
    private const string SolutionFileName = "EmergencyHub.sln";
    private const string SourceFolderName = "src";

    private static readonly string[] BuildOutputFolders = ["bin", "obj"];

    /// <summary>
    /// 검사 대상 목록에 넣지 않는 src 프로젝트. BuildingBlocks도 서비스도 아닌 호스트 공용 프로젝트다(ADR-0011, <see cref="ProductNames.OwnershipOf"/>가 Unknown).
    /// </summary>
    public static IReadOnlyList<string> Excluded { get; } = [ProductNames.ServiceDefaults, ProductNames.AppHost];

    /// <summary>저장소 루트(솔루션 파일이 있는 폴더) 아래 <c>src</c>의 프로젝트 이름(csproj 파일 이름). bin · obj 아래는 뺀다.</summary>
    /// <returns>이름 순으로 정렬한 프로젝트 이름.</returns>
    /// <exception cref="InvalidOperationException">테스트 출력 폴더 위쪽에서 솔루션 파일을 찾지 못한 경우.</exception>
    public static IReadOnlyList<string> InSourceTree()
    {
        var source = Path.Combine(FindRepositoryRoot(), SourceFolderName);

        return
        [
            .. Directory.EnumerateFiles(source, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !IsUnderBuildOutput(Path.GetRelativePath(source, path)))
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .Order(StringComparer.Ordinal),
        ];
    }

    /// <summary>src 프로젝트 중 제외 대상(<see cref="Excluded"/>)이 아니면서 목록에 없는 것을 찾는다.</summary>
    /// <param name="sourceProjects">src 프로젝트 이름.</param>
    /// <param name="listed">검사 대상 목록의 어셈블리 이름.</param>
    /// <returns>빠진 프로젝트 이름(정렬, 중복 없음). 없으면 빈 목록.</returns>
    public static IReadOnlyList<string> FindUnlisted(IEnumerable<string> sourceProjects, IEnumerable<string> listed)
    {
        ArgumentNullException.ThrowIfNull(sourceProjects);
        ArgumentNullException.ThrowIfNull(listed);

        var known = listed.Concat(Excluded).ToHashSet(StringComparer.Ordinal);

        return [.. sourceProjects.Where(name => !known.Contains(name)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    private static bool IsUnderBuildOutput(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => BuildOutputFolders.Contains(segment, StringComparer.OrdinalIgnoreCase));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"{SolutionFileName}을(를) {AppContext.BaseDirectory} 위쪽에서 찾지 못했다.");
    }
}
