using System.Reflection;

namespace EmergencyHub.ArchitectureTests.Assemblies;

/// <summary>검사 대상 제품 어셈블리 하나와 그 레이어.</summary>
/// <param name="Assembly">제품 어셈블리.</param>
/// <param name="Layer">레이어.</param>
public sealed record LayerAssembly(Assembly Assembly, ArchitectureLayer Layer)
{
    /// <summary>어셈블리 이름. 루트 네임스페이스와 같다(coding-conventions "네임스페이스 = 프로젝트 루트 네임스페이스 + 폴더 경로").</summary>
    public string Name { get; } = Assembly.GetName().Name!;

    /// <summary>BuildingBlocks · 서비스 구분.</summary>
    public AssemblyOwnership Ownership => ProductNames.OwnershipOf(Name);

    /// <summary>BuildingBlocks 어셈블리이면 <see langword="true"/>, 서비스 어셈블리이면 <see langword="false"/>.</summary>
    public bool IsBuildingBlocks => Ownership == AssemblyOwnership.BuildingBlocks;

    /// <summary>서비스 접두사(<c>EmergencyHub.&lt;Service&gt;</c>). BuildingBlocks면 <see langword="null"/>.</summary>
    public string? ServiceName => ProductNames.ServiceOf(Name);

    /// <inheritdoc />
    public override string ToString() => Name;
}
