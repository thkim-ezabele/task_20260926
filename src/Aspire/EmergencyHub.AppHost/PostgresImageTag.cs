using System.Reflection;

namespace EmergencyHub.AppHost;

/// <summary>
/// PostgreSQL 이미지 태그를 어셈블리 메타데이터에서 읽습니다. 원본은 <c>Directory.Build.props</c>의 <c>EmergencyHubPostgresImageTag</c> 한 곳입니다
/// (database.md "로컬 DB 구성 (AppHost)", package-versions.md "PostgreSQL 이미지"). 통합 테스트(S03-T06)도 같은 키를 씁니다.
/// </summary>
internal static class PostgresImageTag
{
    /// <summary>메타데이터 키입니다. csproj에 <c>EmergencyHubUsesPostgresImage=true</c>를 두면 빌드가 넣습니다.</summary>
    internal const string MetadataKey = "EmergencyHubPostgresImageTag";

    /// <summary>어셈블리에서 태그를 읽습니다.</summary>
    /// <param name="assembly">메타데이터를 가진 어셈블리.</param>
    /// <returns>이미지 태그.</returns>
    /// <exception cref="InvalidOperationException">키가 없거나, 두 개 이상이거나, 값이 비어 있는 경우.</exception>
    internal static string Read(Assembly assembly) => Parse(assembly.GetCustomAttributes<AssemblyMetadataAttribute>());

    /// <summary>메타데이터 목록에서 태그 항목 하나를 찾습니다.</summary>
    /// <param name="attributes">어셈블리 메타데이터.</param>
    /// <returns>이미지 태그.</returns>
    /// <exception cref="InvalidOperationException">키가 없거나, 두 개 이상이거나, 값이 비어 있는 경우.</exception>
    internal static string Parse(IEnumerable<AssemblyMetadataAttribute> attributes)
    {
        var values = attributes.Where(attribute => attribute.Key == MetadataKey).Select(attribute => attribute.Value).ToList();

        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            throw new InvalidOperationException(
                $"어셈블리 메타데이터 '{MetadataKey}'가 비어 있지 않은 값으로 정확히 하나 있어야 합니다(찾은 개수 {values.Count}). csproj에 EmergencyHubUsesPostgresImage=true를 두었는지 확인하세요.");
        }

        return values[0]!;
    }
}
