using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

namespace EmergencyHub.ArchitectureTests.Assemblies;

/// <summary>
/// 아키텍처 테스트가 검사하는 제품 어셈블리 목록. <b>대상 어셈블리는 이 목록 한곳에서만 관리한다</b>(S02-T05).
/// </summary>
/// <remarks>
/// 서비스를 추가할 때는 이 목록에 레이어별로 한 줄씩 넣고 csproj에 프로젝트 참조를 더하면 모든 규칙이 적용된다
/// (Employee는 S03). 테스트 어셈블리(<c>*Tests</c>)는 넣지 않는다: 인수 테스트 조립 때문에 금지 참조를 가질 수 있고
/// (Api.UnitTests → Infrastructure), Controller · 마커 표본이 섞이기 때문이다(S02-T06 인계).
/// </remarks>
public static class ArchitectureAssemblies
{
    /// <summary>검사 대상 전체. 레이어 순서(Domain → Api)로 둔다.</summary>
    public static IReadOnlyList<LayerAssembly> All { get; } =
    [
        new(typeof(Error).Assembly, ArchitectureLayer.Domain),
        new(typeof(ISender).Assembly, ArchitectureLayer.Application),
        new(typeof(RepositoryBase<>).Assembly, ArchitectureLayer.Infrastructure),
        new(typeof(ApiServiceCollectionExtensions).Assembly, ArchitectureLayer.Api),

        // S03: Employee Domain · Application · Infrastructure · Api · MigrationService를 여기에 추가한다.
    ];

    /// <summary>서비스 어셈블리가 하나라도 목록에 있으면 <see langword="true"/>.</summary>
    public static bool HasServiceAssemblies => All.Any(assembly => !assembly.IsBuildingBlocks);

    /// <summary>목록에 있는 서비스 접두사(<c>EmergencyHub.&lt;Service&gt;</c>). 목록 순서를 유지하고 중복을 뺀다.</summary>
    public static IReadOnlyList<string> ServiceNames => [.. All.Select(assembly => assembly.ServiceName).OfType<string>().Distinct(StringComparer.Ordinal)];

    /// <summary>레이어에 속한 어셈블리.</summary>
    /// <param name="layer">레이어.</param>
    /// <returns>목록 순서를 유지한 어셈블리.</returns>
    public static IReadOnlyList<LayerAssembly> In(ArchitectureLayer layer) => [.. All.Where(assembly => assembly.Layer == layer)];

    /// <summary>레이어에 속한 어셈블리 이름(= 루트 네임스페이스). 의존 금지 목록에 쓴다.</summary>
    /// <param name="layer">레이어.</param>
    /// <returns>어셈블리 이름.</returns>
    public static IReadOnlyList<string> NamesIn(ArchitectureLayer layer) => [.. In(layer).Select(assembly => assembly.Name)];
}
