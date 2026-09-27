using EmergencyHub.BuildingBlocks.Api.DependencyInjection;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Acceptance;

// ADR-0024 의존성 규칙 표: BuildingBlocks.Api는 Application · Domain만 참조하고 Infrastructure · EF Core · Npgsql은 참조하지 않는다(S02-T08 인계).
// 이 테스트 프로젝트는 인수 테스트 조립 때문에 Infrastructure를 참조하므로, 테스트 어셈블리가 아니라 제품 어셈블리의 참조 목록을 본다.
// NetArchTest 규칙(아키텍처 테스트 프로젝트)은 S03에서 만든다. 여기서는 빌드 산출물의 직접 참조만 확인한다.
[Trait("FR", "PRD-001/FR-07")]
public sealed class ApiAssemblyAcceptanceTests
{
    private static readonly string[] ForbiddenPrefixes =
    [
        "EmergencyHub.BuildingBlocks.Infrastructure",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
    ];

    [Fact]
    public void ApiAssembly_ReferencesApplicationAndDomainButNoInfrastructureEfCoreOrNpgsql()
    {
        var references = typeof(ApiServiceCollectionExtensions).Assembly.GetReferencedAssemblies().Select(name => name.Name!).ToList();

        // 공허 통과 방지: 허용된 BuildingBlocks 참조는 실제로 보여야 한다.
        references.Should().Contain(["EmergencyHub.BuildingBlocks.Application", "EmergencyHub.BuildingBlocks.Domain"]);
        references.Should().NotContain(name => ForbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)));
    }
}
