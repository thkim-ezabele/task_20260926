using System.Text.Json;

namespace EmergencyHub.ArchitectureTests.References;

/// <summary>
/// 이 테스트 어셈블리의 deps.json에서 제품 프로젝트마다 <b>선언한</b> 직접 참조(프로젝트 · 패키지)를 읽는다.
/// </summary>
/// <remarks>
/// C# 컴파일러는 쓰지 않는 참조를 어셈블리 메타데이터에 남기지 않으므로 형식 의존 검사(NetArchTest)로는 csproj의 쓰지 않는
/// 참조를 볼 수 없다. deps.json은 빌드가 만든 의존 그래프라 csproj에 적은 ProjectReference · PackageReference가 그대로 남는다.
/// FrameworkReference(공유 프레임워크)는 나오지 않는다.
/// </remarks>
public static class DeclaredReferences
{
    private static readonly Lazy<Dictionary<string, IReadOnlyList<string>>> ProjectDependencies = new(Load);

    /// <summary>프로젝트 이름 → 선언한 직접 참조 이름. 프로젝트가 의존 그래프에 없으면 <see langword="null"/>.</summary>
    /// <param name="projectName">프로젝트(어셈블리) 이름.</param>
    /// <returns>직접 참조 이름.</returns>
    public static IReadOnlyList<string>? Of(string projectName) =>
        ProjectDependencies.Value.TryGetValue(projectName, out var dependencies) ? dependencies : null;

    private static Dictionary<string, IReadOnlyList<string>> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"{typeof(DeclaredReferences).Assembly.GetName().Name}.deps.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var target = root.GetProperty("targets").EnumerateObject().First().Value;

        var projects = root.GetProperty("libraries").EnumerateObject()
            .Where(library => library.Value.GetProperty("type").GetString() == "project")
            .Select(library => library.Name);

        return projects.ToDictionary(
            key => key[..key.IndexOf('/', StringComparison.Ordinal)],
            key => (IReadOnlyList<string>)[.. DependencyNames(target.GetProperty(key))],
            StringComparer.Ordinal);
    }

    private static List<string> DependencyNames(JsonElement entry) =>
        entry.TryGetProperty("dependencies", out var dependencies)
            ? [.. dependencies.EnumerateObject().Select(dependency => dependency.Name)]
            : [];
}
