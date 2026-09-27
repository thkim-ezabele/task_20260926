namespace EmergencyHub.ArchitectureTests;

// 레이어 의존 규칙을 제품 어셈블리(ArchitectureAssemblies)에 적용한다. 규칙마다 테스트 1개, 대상 형식 1개 이상 단언(RuleCheck.ShouldPassOnProduct).
// 규칙 원본: ADR-0024 의존성 규칙 표(S02-T05 규칙 원본) + clean-architecture "의존성 규칙". 각 규칙의 원본 행은 DependencyRules의 Source.
// 위반 예시로 실패하는지는 DependencyRuleSampleTests가 같은 규칙 객체로 확인한다.
[Trait("FR", "PRD-001/FR-09")]
[Trait("NFR", "PRD-001/NFR-02")]
public sealed class DependencyRuleTests
{
    // ADR-0024 표 BuildingBlocks.Domain · <Service>.Domain 행, clean-architecture 의존성 규칙
    [Fact]
    public void DomainDependsOnlyOnSystemAndDomain_ProductAssemblies_Holds() =>
        DependencyRules.DomainDependsOnlyOnSystemAndDomain.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 BuildingBlocks.Application · <Service>.Application 행, clean-architecture(Application은 Infrastructure를 모른다)
    [Fact]
    public void ApplicationDoesNotDependOnInfrastructure_ProductAssemblies_Holds() =>
        DependencyRules.ApplicationDoesNotDependOnInfrastructure.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 BuildingBlocks.Application · <Service>.Application 행(Api 계열)
    [Fact]
    public void ApplicationDoesNotDependOnApi_ProductAssemblies_Holds() =>
        DependencyRules.ApplicationDoesNotDependOnApi.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 BuildingBlocks.Application · <Service>.Application 행(EF Core · Npgsql · Scrutor · ASP.NET Core)
    [Fact]
    public void ApplicationDoesNotDependOnFrameworks_ProductAssemblies_Holds() =>
        DependencyRules.ApplicationDoesNotDependOnFrameworks.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 BuildingBlocks.Infrastructure · <Service>.Infrastructure 행(Api 계열)
    [Fact]
    public void InfrastructureDoesNotDependOnApi_ProductAssemblies_Holds() =>
        DependencyRules.InfrastructureDoesNotDependOnApi.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 BuildingBlocks.Infrastructure · <Service>.Infrastructure 행(ASP.NET Core, 형식 의존 기준)
    [Fact]
    public void InfrastructureDoesNotDependOnAspNetCore_ProductAssemblies_Holds() =>
        DependencyRules.InfrastructureDoesNotDependOnAspNetCore.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 BuildingBlocks.Api 행(Infrastructure 계열 · EF Core · Npgsql)
    [Fact]
    public void BuildingBlocksApiDoesNotDependOnInfrastructureOrDatabase_ProductAssemblies_Holds() =>
        DependencyRules.BuildingBlocksApiDoesNotDependOnInfrastructureOrDatabase.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 <Service>.Api 행(Controller에서 Infrastructure 타입 · Repository 사용 금지), clean-architecture(Api는 Infrastructure를 DI 등록에만)
    // 대상은 서비스 Api의 Controller(S03 전 건너뜀)
    [Fact]
    public void ControllersDoNotUseInfrastructureOrRepositories_ProductAssemblies_Holds() =>
        DependencyRules.ControllersDoNotUseInfrastructureOrRepositories.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 <Service>.MigrationService 행(<Service>.Api · BuildingBlocks.Api) — 대상은 서비스 MigrationService(S03 전 건너뜀)
    [Fact]
    public void MigrationServiceDoesNotDependOnApi_ProductAssemblies_Holds() =>
        DependencyRules.MigrationServiceDoesNotDependOnApi.CheckProduct().ShouldPassOnProduct();

    // ADR-0024 표 아래 "서비스끼리는 프로젝트를 참조하지 않는다", clean-architecture 의존성 규칙, ADR-0024 <Service>.Domain 행(BuildingBlocks.Domain만)
    // 서비스마다 규칙 1개(다른 서비스 접두사 금지). 서비스가 MinimumServiceCount(2)개 미만이면 건너뜀(S03은 Employee 1개).
    [Fact]
    public void ServicesDoNotDependOnOtherServices_ProductAssemblies_Holds() =>
        DependencyRules.ServicesDoNotDependOnOtherServices.ShouldPassOnProduct();
}
