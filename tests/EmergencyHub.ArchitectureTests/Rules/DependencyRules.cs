using EmergencyHub.ArchitectureTests.References;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 레이어 의존 규칙(형식 의존 기준). 원본: <b>ADR-0024 의존성 규칙 표</b>(S02-T05 설계 원본)와 그 표를 구현에 맞춰 옮긴
/// clean-architecture "의존성 규칙 표"(S04-T02, 규칙 이름 열이 이 클래스의 속성과 1:1).
/// </summary>
/// <remarks>
/// NetArchTest는 형식 의존(필드 · 속성 · 매개변수 · 기반 형식 · 특성 · 메서드 본문 IL)을 검사한다. 쓰지 않는 프로젝트 · 패키지 참조는
/// 컴파일된 어셈블리에 흔적이 남지 않으므로 <see cref="DeclaredReferenceRules"/>가 deps.json의 선언 참조로 따로 막는다.
/// "Infrastructure 계열 ↛ ASP.NET Core"는 ServiceDefaults가 공유 프레임워크를 참조하므로 프로젝트 참조가 아니라 형식 의존으로 단언한다(S02-T08 인계).
/// </remarks>
public static class DependencyRules
{
    private const string ServiceIsolationSource =
        "clean-architecture · ADR-0024 의존성 규칙 표 아래(서비스끼리는 프로젝트를 참조하지 않는다) · <Service>.Domain 행(BuildingBlocks.Domain만), clean-architecture 의존성 규칙(서비스끼리는 프로젝트를 참조하지 않는다. 공유는 BuildingBlocks만)";

    private static readonly Type[] RepositoryMarkers = [typeof(IRepository), typeof(IReadRepository)];

    /// <summary>
    /// Domain은 System(BCL)과 Domain 레이어만 의존하고 직렬화 라이브러리도 쓰지 않는다.
    /// 서비스 Domain끼리의 의존(<c>&lt;Service&gt;.Domain</c>은 BuildingBlocks.Domain만)은 <see cref="ServicesDoNotDependOnOtherServices"/>가 막는다.
    /// </summary>
    public static ArchitectureRule DomainDependsOnlyOnSystemAndDomain { get; } = new(
        "Domain ↛ 다른 모든 프로젝트 · 프레임워크 · 직렬화 라이브러리",
        "clean-architecture · ADR-0024 의존성 규칙 표 BuildingBlocks.Domain · <Service>.Domain 행, clean-architecture 의존성 규칙(Domain은 아무것도 참조하지 않고 프레임워크에 의존하지 않는다)",
        RuleScope.Layer(ArchitectureLayer.Domain),
        ArchitectureRule.AllTypes,
        conditions => conditions
            .OnlyHaveDependenciesOn(["System", .. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Domain)])
            .And()
            .NotHaveDependencyOnAny(ForbiddenDependencies.Combine(ForbiddenDependencies.Serialization)));

    /// <summary>Application은 Infrastructure 계열을 모른다.</summary>
    public static ArchitectureRule ApplicationDoesNotDependOnInfrastructure { get; } = new(
        "Application ↛ Infrastructure 계열",
        "clean-architecture · ADR-0024 의존성 규칙 표 BuildingBlocks.Application · <Service>.Application 행, clean-architecture 의존성 규칙(Application은 Infrastructure를 모른다)",
        RuleScope.Layer(ArchitectureLayer.Application),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny([.. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Infrastructure)]));

    /// <summary>Application은 Api 계열을 모른다.</summary>
    public static ArchitectureRule ApplicationDoesNotDependOnApi { get; } = new(
        "Application ↛ Api 계열",
        "clean-architecture · ADR-0024 의존성 규칙 표 BuildingBlocks.Application(BuildingBlocks.Api) · <Service>.Application(Api 계열) 행",
        RuleScope.Layer(ArchitectureLayer.Application),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny([.. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Api)]));

    /// <summary>Application은 EF Core · Npgsql · Scrutor · ASP.NET Core를 쓰지 않는다(FluentValidation · Microsoft.Extensions.*.Abstractions는 허용).</summary>
    public static ArchitectureRule ApplicationDoesNotDependOnFrameworks { get; } = new(
        "Application ↛ EF Core · Npgsql · Scrutor · ASP.NET Core",
        "clean-architecture · ADR-0024 의존성 규칙 표 BuildingBlocks.Application · <Service>.Application 행, ADR-0017(Scrutor는 BuildingBlocks.Infrastructure만)",
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
        "clean-architecture · ADR-0024 의존성 규칙 표 BuildingBlocks.Infrastructure(BuildingBlocks.Api) · <Service>.Infrastructure(<Service>.Api, BuildingBlocks.Api) 행",
        RuleScope.Layer(ArchitectureLayer.Infrastructure),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny([.. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Api)]));

    /// <summary>Infrastructure 계열은 ASP.NET Core(웹 API 규약 · Swashbuckle) 형식을 쓰지 않는다.</summary>
    public static ArchitectureRule InfrastructureDoesNotDependOnAspNetCore { get; } = new(
        "Infrastructure 계열 ↛ ASP.NET Core(Microsoft.AspNetCore.* · Swashbuckle · Microsoft.OpenApi)",
        "clean-architecture · ADR-0024 의존성 규칙 표 BuildingBlocks.Infrastructure · <Service>.Infrastructure 행(아키텍처 테스트로 강제)",
        RuleScope.Layer(ArchitectureLayer.Infrastructure),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny(ForbiddenDependencies.Combine(ForbiddenDependencies.AspNetCore)));

    /// <summary>
    /// BuildingBlocks.Api는 Infrastructure 계열 · EF Core · Npgsql을 모른다(23505 변환 책임이 Infrastructure에만 있음을 보장).
    /// 서비스 Api는 DI 등록에 Infrastructure를 쓸 수 있으므로 BuildingBlocks.Api만 대상이다.
    /// </summary>
    public static ArchitectureRule BuildingBlocksApiDoesNotDependOnInfrastructureOrDatabase { get; } = new(
        "BuildingBlocks.Api ↛ Infrastructure 계열 · EF Core · Npgsql",
        "clean-architecture · ADR-0024 의존성 규칙 표 BuildingBlocks.Api 행(아키텍처 테스트로 강제)",
        RuleScope.Layer(ArchitectureLayer.Api, AssemblyOwnership.BuildingBlocks),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny(ForbiddenDependencies.Combine(
            ArchitectureAssemblies.NamesIn(ArchitectureLayer.Infrastructure),
            ForbiddenDependencies.EfCore,
            ForbiddenDependencies.Npgsql)));

    /// <summary>
    /// 서비스 Api의 Controller는 Infrastructure 계열 형식과 Repository(<see cref="IRepository"/> · <see cref="IReadRepository"/> 파생)를 쓰지 않는다.
    /// Api의 Infrastructure 사용은 DI 등록 코드에만 허용되므로 대상은 Controller(<see cref="ControllerBase"/> 파생)뿐이다.
    /// </summary>
    /// <remarks>
    /// Repository 인터페이스 이름은 미리 알 수 없으므로 마커에 할당 가능한 형식이 시그니처(생성자 · 메서드 매개변수 · 반환 · 필드 · 속성)에
    /// 있는지 리플렉션으로 본다. 메서드 본문에서 서비스 로케이터로 꺼내는 Repository는 잡지 못한다(reviewer 판정).
    /// Infrastructure 형식은 메서드 본문 IL까지 형식 의존으로 잡는다.
    /// </remarks>
    public static ArchitectureRule ControllersDoNotUseInfrastructureOrRepositories { get; } = new(
        "<Service>.Api Controller ↛ Infrastructure 계열 형식 · Repository(IRepository · IReadRepository 파생)",
        "clean-architecture · ADR-0024 의존성 규칙 표 <Service>.Api 행(Controller에서 Infrastructure 타입 · Repository 사용 금지), clean-architecture 의존성 규칙(Api는 Infrastructure를 DI 등록에만 쓰고 엔드포인트에서 직접 쓰지 않는다)",
        RuleScope.Layer(ArchitectureLayer.Api, AssemblyOwnership.Service),
        scope => scope.And().MeetCustomRule(new TypeRule(IsController)),
        conditions => conditions
            .NotHaveDependencyOnAny(ForbiddenDependencies.Combine(
                ArchitectureAssemblies.NamesIn(ArchitectureLayer.Infrastructure),
                [.. RepositoryMarkers.Select(marker => marker.FullName!)]))
            .And()
            .MeetCustomRule(new TypeRule(type => !TypeInspection.SignaturesUseAnyOf(type, RepositoryMarkers))),
        TargetsOnlyInServices: true);

    /// <summary>서비스 MigrationService는 Api 계열(<c>&lt;Service&gt;.Api</c> · BuildingBlocks.Api)을 모른다.</summary>
    public static ArchitectureRule MigrationServiceDoesNotDependOnApi { get; } = new(
        "<Service>.MigrationService ↛ Api 계열(<Service>.Api · BuildingBlocks.Api)",
        "clean-architecture · ADR-0024 의존성 규칙 표 <Service>.MigrationService 행",
        RuleScope.Layer(ArchitectureLayer.MigrationService),
        ArchitectureRule.AllTypes,
        conditions => conditions.NotHaveDependencyOnAny([.. ArchitectureAssemblies.NamesIn(ArchitectureLayer.Api)]),
        TargetsOnlyInServices: true);

    /// <summary>
    /// 서비스끼리는 의존하지 않는다: 서비스 어셈블리(모든 레이어)는 다른 서비스 접두사(<c>EmergencyHub.&lt;Other&gt;</c>)의 형식을 쓰지 않는다.
    /// 서비스마다 규칙 하나이고, 서비스가 2개 미만이면 건너뛴다. 선언 참조 쪽은 <see cref="DeclaredReferenceRules"/>가 서비스 수와 관계없이 막는다.
    /// </summary>
    public static ServiceRuleSet ServicesDoNotDependOnOtherServices { get; } = new(
        "서비스 ↛ 다른 서비스",
        ServiceIsolationSource,
        MinimumServiceCount: 2,
        ServiceDoesNotDependOnOtherServices);

    /// <summary>서비스 하나가 다른 서비스 형식을 쓰지 않는다는 규칙.</summary>
    /// <param name="service">서비스 접두사(제품 범위를 고른다).</param>
    /// <param name="otherServices">금지할 다른 서비스 접두사.</param>
    /// <returns>규칙.</returns>
    public static ArchitectureRule ServiceDoesNotDependOnOtherServices(string service, IReadOnlyList<string> otherServices)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(otherServices);

        return new(
            $"{service} ↛ 다른 서비스({string.Join(", ", otherServices)})",
            ServiceIsolationSource,
            RuleScope.Service(service),
            ArchitectureRule.AllTypes,
            conditions => conditions.NotHaveDependencyOnAny([.. otherServices]),
            TargetsOnlyInServices: true);
    }

    private static bool IsController(Type type) => TypeInspection.IsConcreteClass(type) && typeof(ControllerBase).IsAssignableFrom(type);
}
