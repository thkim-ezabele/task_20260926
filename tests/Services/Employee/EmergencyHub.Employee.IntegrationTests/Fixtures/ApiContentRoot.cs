using System.Text.Json;
using System.Text.Json.Nodes;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// <see cref="EmployeeApiFactory"/>의 콘텐츠 루트(임시 폴더)를 만듭니다. Api의 <c>appsettings*.json</c>을 복사하면서 <c>Serilog:WriteTo</c>만 뺍니다.
/// </summary>
/// <remarks>
/// 운영 설정의 콘솔 · 파일 싱크(<c>logs/employee-*.json</c>)를 테스트에서 쓰지 않으면서, 최소 수준 · 범주 재정의(<c>Serilog:MinimumLevel</c>)와
/// 공통 속성(<c>Serilog:Properties</c>)은 운영과 같게 둡니다. 설정 키는 지울 수 없어(메모리 설정은 덮어쓰기만 가능) 파일 단계에서 뺍니다.
/// </remarks>
public static class ApiContentRoot
{
    private const string SettingsPattern = "appsettings*.json";
    private const string BaseSettingsFile = "appsettings.json";
    private const string SerilogSection = "Serilog";
    private const string WriteToKey = "WriteTo";

    private static readonly JsonDocumentOptions ReadOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>임시 콘텐츠 루트를 만들고 설정 파일을 싱크 없이 복사합니다.</summary>
    /// <param name="sourceDirectory">Api 프로젝트 폴더(<see cref="RepositoryFiles.EmployeeApiDirectory"/>).</param>
    /// <returns>만든 폴더 경로. 호출자가 <see cref="Delete"/>로 지웁니다.</returns>
    /// <exception cref="FileNotFoundException"><paramref name="sourceDirectory"/>에 <c>appsettings.json</c>이 없는 경우.</exception>
    public static string Create(string sourceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);

        if (!File.Exists(Path.Combine(sourceDirectory, BaseSettingsFile)))
        {
            throw new FileNotFoundException($"{BaseSettingsFile}을 {sourceDirectory}에서 찾지 못했습니다.", BaseSettingsFile);
        }

        var target = Directory.CreateTempSubdirectory("employee-api-it-").FullName;
        foreach (var file in Directory.EnumerateFiles(sourceDirectory, SettingsPattern, SearchOption.TopDirectoryOnly))
        {
            File.WriteAllText(Path.Combine(target, Path.GetFileName(file)), RemoveSerilogSinks(File.ReadAllText(file)));
        }

        return target;
    }

    /// <summary>설정 JSON에서 <c>Serilog:WriteTo</c>를 뺍니다(키 이름은 설정과 같이 대소문자 무시). 나머지는 그대로 둡니다.</summary>
    /// <param name="json">설정 파일 내용.</param>
    /// <returns>싱크를 뺀 JSON.</returns>
    /// <exception cref="JsonException">JSON 객체가 아닌 경우.</exception>
    public static string RemoveSerilogSinks(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var root = JsonNode.Parse(json, documentOptions: ReadOptions) as JsonObject
            ?? throw new JsonException("설정 파일의 최상위가 JSON 객체가 아닙니다.");

        if (FindProperty(root, SerilogSection) is JsonObject serilog && FindKey(serilog, WriteToKey) is { } writeTo)
        {
            serilog.Remove(writeTo);
        }

        return root.ToJsonString(WriteOptions);
    }

    /// <summary>콘텐츠 루트를 지웁니다. 없으면 아무것도 하지 않습니다.</summary>
    /// <param name="path">콘텐츠 루트.</param>
    public static void Delete(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static JsonNode? FindProperty(JsonObject parent, string name) =>
        FindKey(parent, name) is { } key ? parent[key] : null;

    private static string? FindKey(JsonObject parent, string name) =>
        parent.Select(pair => pair.Key).FirstOrDefault(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
}
