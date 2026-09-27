namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 레이어 의존 규칙(형식 의존 기준). 원본: <b>ADR-0024 의존성 규칙 표</b>(S02-T05 규칙 원본)와
/// clean-architecture "의존성 규칙". clean-architecture 표에는 S04-T02에서 Api 행이 반영된다.
/// </summary>
/// <remarks>
/// NetArchTest는 형식 의존(필드 · 속성 · 매개변수 · 기반 형식 · 특성 · 메서드 본문 IL)을 검사한다. 쓰지 않는 프로젝트 · 패키지 참조는
/// 컴파일된 어셈블리에 흔적이 남지 않으므로 <see cref="DeclaredReferenceRules"/>가 deps.json의 선언 참조로 따로 막는다.
/// "Infrastructure 계열 ↛ ASP.NET Core"는 ServiceDefaults가 공유 프레임워크를 참조하므로 프로젝트 참조가 아니라 형식 의존으로 단언한다(S02-T08 인계).
/// </remarks>
public static class DependencyRules
{
    /// <summary>Domain은 System(BCL)과 Domain 레이어만 의존하고 직렬화 라이브러리도 쓰지 않는다.</summary>
    public static ArchitectureRule DomainDependsOnlyOnSystemAndDomain { get; } = new(
        "Domain ↛ 다른 모든 프로젝트 · 프레임워크 · 직렬화 라이브러리",
        "ADR-0024 의존성 규칙 표 BuildingBlocks.Domain · <Service>.Domain 행, clean-architecture 의존성 규칙(Domain은 아무것도 참조하지 않고 프레임워크에 의존하지 않는다)",
        RuleScope.Layer(ArchitectureLayer.Domain),
        ArchitectureRule.AllTypes,
        conditions => conditions
            .OnlyHaveDependenciesOn(["System", .. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Domain)])
            .And()
            .NotHaveDependencyOnAny(ForbiddenDependencies.Combine(ForbiddenDependencies.Serialization)));

    /// <summary>Application은 Infrastructure 계열을 모른다.</summary>
    public static ArchitectureRule ApplicationDoesNotDependOnInfrastructure { get; } = new(
        "Application ↛ Infrastructure 계열",
        "ADR-0024 의존성 규칙 표 BuildingBlocks.Application · <Service>.Application 행, clean-architecture 의존성 규칙(Application은 Infrastructure를 모른다)",
        RuleScope.Layer(ArchitectureLayer.Application),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny([.. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Infrastructure)]));

    /// <summary>Application은 Api 계열을 모른다.</summary>
    public static ArchitectureRule ApplicationDoesNotDependOnApi { get; } = new(
        "Application ↛ Api 계열",
        "ADR-0024 의존성 규칙 표 BuildingBlocks.Application(BuildingBlocks.Api) · <Service>.Application(Api 계열) 행",
        RuleScope.Layer(ArchitectureLayer.Application),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny([.. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Api)]));

    /// <summary>Application은 EF Core · Npgsql · Scrutor · ASP.NET Core를 쓰지 않는다(FluentValidation · Microsoft.Extensions.*.Abstractions는 허용).</summary>
    public static ArchitectureRule ApplicationDoesNotDependOnFrameworks { get; } = new(
        "Application ↛ EF Core · Npgsql · Scrutor · ASP.NET Core",
        "ADR-0024 의존성 규칙 표 BuildingBlocks.Application · <Service>.Application 행, ADR-0017(Scrutor는 BuildingBlocks.Infrastructure만)",
        RuleScope.Layer(ArchitectureLayer.Application),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny(ForbiddenDependencies.Combine(
            ForbiddenDependencies.EfCore,
            ForbiddenDependencies.Npgsql,
            ForbiddenDependencies.Scrutor,
            ForbiddenDependencies.AspNetCore)));

    /// <summary>Infrastructure 계열은 Api 계열을 모른다.</summary>
    public static ArchitectureRule InfrastructureDoesNotDependOnApi { get; } = new(
        "Infrastructure 계열 ↛ Api 계열",
        "ADR-0024 의존성 규칙 표 BuildingBlocks.Infrastructure(BuildingBlocks.Api) · <Service>.Infrastructure(<Service>.Api, BuildingBlocks.Api) 행",
        RuleScope.Layer(ArchitectureLayer.Infrastructure),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny([.. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Api)]));

    /// <summary>Infrastructure 계열은 ASP.NET Core(웹 API 규약 · Swashbuckle) 형식을 쓰지 않는다.</summary>
    public static ArchitectureRule InfrastructureDoesNotDependOnAspNetCore { get; } = new(
        "Infrastructure 계열 ↛ ASP.NET Core(Microsoft.AspNetCore.* · Swashbuckle · Microsoft.OpenApi)",
        "ADR-0024 의존성 규칙 표 BuildingBlocks.Infrastructure · <Service>.Infrastructure 행(아키텍처 테스트로 강제)",
        RuleScope.Layer(ArchitectureLayer.Infrastructure),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny(ForbiddenDependencies.Combine(ForbiddenDependencies.AspNetCore)));

    /// <summary>
    /// BuildingBlocks.Api는 Infrastructure 계열 · EF Core · Npgsql을 모른다(23505 변환 책임이 Infrastructure에만 있음을 보장).
    /// 서비스 Api는 DI 등록에 Infrastructure를 쓸 수 있으므로 BuildingBlocks.Api만 대상이다.
    /// </summary>
    public static ArchitectureRule BuildingBlocksApiDoesNotDependOnInfrastructureOrDatabase { get; } = new(
        "BuildingBlocks.Api ↛ Infrastructure 계열 · EF Core · Npgsql",
        "ADR-0024 의존성 규칙 표 BuildingBlocks.Api 행(아키텍처 테스트로 강제)",
        RuleScope.Layer(ArchitectureLayer.Api, buildingBlocksOnly: true),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny(ForbiddenDependencies.Combine(
            ArchitectureAssemblies.NamesIn(ArchitectureLayer.Infrastructure),
            ForbiddenDependencies.EfCore,
            ForbiddenDependencies.Npgsql)));
}
