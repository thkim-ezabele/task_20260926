namespace EmergencyHub.ArchitectureTests.Assemblies;

/// <summary>
/// 제품 프로젝트 이름(= 어셈블리 이름 = 루트 네임스페이스) 규칙. 서비스는 <c>EmergencyHub.&lt;Service&gt;.&lt;Layer&gt;</c>이고
/// 점 단위 앞 두 이름(<c>EmergencyHub.&lt;Service&gt;</c>)이 서비스 접두사다(clean-architecture "저장소 디렉터리 구조").
/// </summary>
public static class ProductNames
{
    /// <summary>제품 루트 이름.</summary>
    public const string Root = "EmergencyHub";

    /// <summary>공유 빌딩 블록 접두사.</summary>
    public const string BuildingBlocks = "EmergencyHub.BuildingBlocks";

    /// <summary>서비스가 함께 쓰는 Aspire 서비스 기본값 프로젝트(ADR-0011). 서비스가 아니다.</summary>
    public const string ServiceDefaults = "EmergencyHub.ServiceDefaults";

    /// <summary>제품 프로젝트 이름의 소유를 판별한다.</summary>
    /// <param name="name">프로젝트 이름.</param>
    /// <returns>BuildingBlocks · 서비스 · 그 밖(<see cref="AssemblyOwnership.Unknown"/>).</returns>
    public static AssemblyOwnership OwnershipOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (IsUnder(name, BuildingBlocks))
        {
            return AssemblyOwnership.BuildingBlocks;
        }

        return ServiceOf(name) is null ? AssemblyOwnership.Unknown : AssemblyOwnership.Service;
    }

    /// <summary>서비스 프로젝트 이름의 서비스 접두사(<c>EmergencyHub.&lt;Service&gt;</c>).</summary>
    /// <param name="name">프로젝트 이름.</param>
    /// <returns>서비스 접두사. BuildingBlocks · ServiceDefaults · 제품 밖 이름이면 <see langword="null"/>.</returns>
    public static string? ServiceOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var segments = name.Split('.');

        if (segments.Length < 2 || !string.Equals(segments[0], Root, StringComparison.Ordinal))
        {
            return null;
        }

        var service = $"{segments[0]}.{segments[1]}";

        return service is BuildingBlocks or ServiceDefaults ? null : service;
    }

    /// <summary>이름이 접두사와 같거나 <c>접두사.</c>로 시작하면 <see langword="true"/>(점 단위 비교).</summary>
    /// <param name="name">이름.</param>
    /// <param name="prefix">접두사.</param>
    /// <returns>판별 결과.</returns>
    public static bool IsUnder(string name, string prefix) =>
        string.Equals(name, prefix, StringComparison.Ordinal) || name.StartsWith(prefix + ".", StringComparison.Ordinal);
}
