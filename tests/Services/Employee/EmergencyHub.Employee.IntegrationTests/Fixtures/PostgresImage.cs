using System.Reflection;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// 통합 테스트 PostgreSQL 이미지입니다. 태그는 어셈블리 메타데이터 <c>EmergencyHubPostgresImageTag</c>에서만 읽습니다
/// (원본 <c>Directory.Build.props</c> 한 곳, AppHost <c>PostgresImageTag</c>와 같은 규칙: 정확히 1개 · 비어 있지 않음, TD-004).
/// </summary>
public static class PostgresImage
{
    /// <summary>메타데이터 키입니다. csproj의 <c>EmergencyHubUsesPostgresImage=true</c>가 넣습니다.</summary>
    public const string MetadataKey = "EmergencyHubPostgresImageTag";

    private const string Repository = "postgres";

    /// <summary>이 테스트 어셈블리의 이미지 태그입니다.</summary>
    public static string Tag => Read(typeof(PostgresImage).Assembly);

    /// <summary><c>postgres:&lt;태그&gt;</c> 형태의 이미지 이름입니다.</summary>
    public static string Name => $"{Repository}:{Tag}";

    /// <summary>어셈블리에서 태그를 읽습니다.</summary>
    /// <param name="assembly">메타데이터를 가진 어셈블리.</param>
    /// <returns>이미지 태그.</returns>
    /// <exception cref="InvalidOperationException">키가 없거나, 두 개 이상이거나, 값이 비어 있는 경우.</exception>
    public static string Read(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return Parse(assembly.GetCustomAttributes<AssemblyMetadataAttribute>());
    }

    /// <summary>메타데이터 목록에서 태그 항목 하나를 찾습니다.</summary>
    /// <param name="attributes">어셈블리 메타데이터.</param>
    /// <returns>이미지 태그.</returns>
    /// <exception cref="InvalidOperationException">키가 없거나, 두 개 이상이거나, 값이 비어 있는 경우.</exception>
    public static string Parse(IEnumerable<AssemblyMetadataAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        var values = attributes.Where(attribute => attribute.Key == MetadataKey).Select(attribute => attribute.Value).ToList();

        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            throw new InvalidOperationException(
                $"어셈블리 메타데이터 '{MetadataKey}'가 비어 있지 않은 값으로 정확히 하나 있어야 합니다(찾은 개수 {values.Count}). csproj에 EmergencyHubUsesPostgresImage=true를 두었는지 확인하세요.");
        }

        return values[0]!;
    }
}
