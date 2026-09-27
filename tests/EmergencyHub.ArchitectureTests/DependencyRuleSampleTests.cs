using EmergencyHub.ArchitectureTests.Samples.ApplicationApiDependencies;
using EmergencyHub.ArchitectureTests.Samples.ApplicationFrameworkDependencies;
using EmergencyHub.ArchitectureTests.Samples.ApplicationInfrastructureDependencies;
using EmergencyHub.ArchitectureTests.Samples.BuildingBlocksApiDependencies;
using EmergencyHub.ArchitectureTests.Samples.ControllerDependencies;
using EmergencyHub.ArchitectureTests.Samples.DomainDependencies;
using EmergencyHub.ArchitectureTests.Samples.InfrastructureApiDependencies;
using EmergencyHub.ArchitectureTests.Samples.InfrastructureAspNetCoreDependencies;
using EmergencyHub.ArchitectureTests.Samples.MigrationServiceDependencies;
using EmergencyHub.ArchitectureTests.Samples.ServiceIsolation;
using EmergencyHub.ArchitectureTests.Samples.ServiceIsolationOrdering;

namespace EmergencyHub.ArchitectureTests;

// 의존 규칙이 일부러 어긴 표본 형식을 잡는지 확인한다(DependencyRuleTests와 같은 규칙 객체, 범위만 표본 네임스페이스).
// 표본 네임스페이스 하나 = 규칙 하나. 규칙을 지킨 형식을 함께 두어 오탐이 없음도 확인한다(RuleCheck.ShouldFlagExactly).
// 표본은 이 테스트 어셈블리에 있고, 테스트 어셈블리는 ArchitectureAssemblies(제품 검사 대상)에 없으므로 제품 검사를 깨지 않는다.
[Trait("FR", "PRD-001/FR-09")]
public sealed class DependencyRuleSampleTests
{
    [Fact]
    public void DomainDependsOnlyOnSystemAndDomain_SamplesUsingSerializerFrameworkOrOuterLayer_FlagsOnlyThose() =>
        DependencyRules.DomainDependsOnlyOnSystemAndDomain.Check(RuleScope.Samples<SystemOnlyDomainType>())
            .ShouldFlagExactly(typeof(JsonSerializingDomainType), typeof(LoggingDomainType), typeof(SenderUsingDomainType));

    [Fact]
    public void ApplicationDoesNotDependOnInfrastructure_SampleUsingInfrastructureInMethodBody_FlagsOnlyIt() =>
        DependencyRules.ApplicationDoesNotDependOnInfrastructure.Check(RuleScope.Samples<PortOnlyApplicationType>())
            .ShouldFlagExactly(typeof(IndexNameApplicationType));

    [Fact]
    public void ApplicationDoesNotDependOnApi_SampleCreatingApiResponseType_FlagsOnlyIt() =>
        DependencyRules.ApplicationDoesNotDependOnApi.Check(RuleScope.Samples<ResultReturningApplicationType>())
            .ShouldFlagExactly(typeof(ProblemFieldApplicationType));

    [Fact]
    public void ApplicationDoesNotDependOnFrameworks_SamplesUsingEachForbiddenFramework_FlagsEachButAllowsFluentValidation() =>
        DependencyRules.ApplicationDoesNotDependOnFrameworks.Check(RuleScope.Samples<FluentValidationApplicationType>())
            .ShouldFlagExactly(
                typeof(EfCoreApplicationType),
                typeof(NpgsqlApplicationType),
                typeof(ScrutorApplicationType),
                typeof(AspNetCoreApplicationType));

    [Fact]
    public void InfrastructureDoesNotDependOnApi_SampleUsingApiType_FlagsOnlyItButAllowsEfCore() =>
        DependencyRules.InfrastructureDoesNotDependOnApi.Check(RuleScope.Samples<DatabaseInfrastructureType>())
            .ShouldFlagExactly(typeof(ProblemFieldInfrastructureType));

    [Fact]
    public void InfrastructureDoesNotDependOnAspNetCore_SamplesUsingHttpContextOrSwashbuckle_FlagsBothButAllowsNpgsql() =>
        DependencyRules.InfrastructureDoesNotDependOnAspNetCore.Check(RuleScope.Samples<DatabaseOnlyInfrastructureType>())
            .ShouldFlagExactly(typeof(HttpContextInfrastructureType), typeof(SwaggerOptionsInfrastructureType));

    [Fact]
    public void BuildingBlocksApiDoesNotDependOnInfrastructureOrDatabase_SamplesUsingInfrastructureEfCoreOrNpgsqlTypes_FlagsEach() =>
        DependencyRules.BuildingBlocksApiDoesNotDependOnInfrastructureOrDatabase.Check(RuleScope.Samples<ResultMappingApiType>())
            .ShouldFlagExactly(typeof(RegistryUsingApiType), typeof(DbUpdateCatchingApiType), typeof(PostgresCatchingApiType));

    // 대상 선택: Controller만(같은 네임스페이스의 DI 등록 정적 클래스 SampleApiRegistration은 Infrastructure를 써도 대상이 아님).
    // 위반 경로: 생성자 주입(IRepository 파생), 액션 [FromServices](IReadRepository 파생), 메서드 본문의 Infrastructure 형식.
    [Fact]
    public void ControllersDoNotUseInfrastructureOrRepositories_SamplesInjectingRepositoriesOrUsingInfrastructure_FlagsEachButAllowsSenderAndRegistration() =>
        DependencyRules.ControllersDoNotUseInfrastructureOrRepositories.Check(RuleScope.Samples<SenderOnlySampleController>())
            .ShouldFlagExactly(
                typeof(RepositoryInjectedSampleController),
                typeof(ReadRepositoryActionSampleController),
                typeof(InfrastructureUsingSampleController));

    [Fact]
    public void ControllersDoNotUseInfrastructureOrRepositories_Samples_DoesNotSelectNonControllerRegistration() =>
        DependencyRules.ControllersDoNotUseInfrastructureOrRepositories.Check(RuleScope.Samples<SenderOnlySampleController>())
            .TargetNames.Should().NotContain(typeof(SampleApiRegistration).FullName!, "DI 등록 코드는 Infrastructure를 쓸 수 있다(ADR-0024 <Service>.Api 행)");

    [Fact]
    public void MigrationServiceDoesNotDependOnApi_SamplesUsingApiTypeInPropertyOrMethodBody_FlagsBothButAllowsInfrastructure() =>
        DependencyRules.MigrationServiceDoesNotDependOnApi.Check(RuleScope.Samples<InfrastructureOnlyMigrationType>())
            .ShouldFlagExactly(typeof(ProblemFieldMigrationType), typeof(ProblemResultMigrationType));

    // 표본 서비스 = ServiceIsolation 네임스페이스, 다른 서비스 = ServiceIsolationOrdering 네임스페이스.
    // 지킨 예는 BuildingBlocks 형식과 접두사만 같은 ServiceIsolationOrderingReports 형식을 쓴다(점 경계 비교 확인).
    [Fact]
    public void ServiceDoesNotDependOnOtherServices_SamplesUsingOtherServiceType_FlagsOnlyThoseButAllowsBuildingBlocksAndLookalikeNamespace() =>
        DependencyRules.ServiceDoesNotDependOnOtherServices(SampleService, [OtherSampleService])
            .Check(RuleScope.Samples<BuildingBlocksOnlyServiceType>())
            .ShouldFlagExactly(typeof(OtherServicePropertyType), typeof(OtherServiceMethodBodyType));

    [Fact]
    public void ServicesDoNotDependOnOtherServices_ForServices_MakesOneRulePerServiceForbiddingOnlyTheOthers()
    {
        var rules = DependencyRules.ServicesDoNotDependOnOtherServices.ForServices(["EmergencyHub.Employee", "EmergencyHub.Notification", "EmergencyHub.Emergency"]);

        rules.Select(rule => rule.Name).Should().Equal(
            "EmergencyHub.Employee ↛ 다른 서비스(EmergencyHub.Notification, EmergencyHub.Emergency)",
            "EmergencyHub.Notification ↛ 다른 서비스(EmergencyHub.Employee, EmergencyHub.Emergency)",
            "EmergencyHub.Emergency ↛ 다른 서비스(EmergencyHub.Employee, EmergencyHub.Notification)");
    }

    [Fact]
    public void ServicesDoNotDependOnOtherServices_MinimumServiceCount_IsTwo() =>
        DependencyRules.ServicesDoNotDependOnOtherServices.MinimumServiceCount.Should().Be(2, "서비스가 1개면 금지할 다른 서비스가 없어 공허 통과다");

    [Fact]
    public void ServicesDoNotDependOnOtherServices_SampleScopeWithoutOtherServiceUse_Passes() =>
        DependencyRules.ServiceDoesNotDependOnOtherServices(OtherSampleService, [SampleService])
            .Check(RuleScope.Samples<OrderSnapshot>())
            .IsSuccessful.Should().BeTrue("다른 서비스 형식을 쓰지 않는 서비스는 규칙을 지킨다");

    private static string SampleService => typeof(BuildingBlocksOnlyServiceType).Namespace!;

    private static string OtherSampleService => typeof(OrderSnapshot).Namespace!;
}
