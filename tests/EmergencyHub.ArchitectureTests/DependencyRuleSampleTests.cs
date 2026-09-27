using EmergencyHub.ArchitectureTests.Samples.ApplicationApiDependencies;
using EmergencyHub.ArchitectureTests.Samples.ApplicationFrameworkDependencies;
using EmergencyHub.ArchitectureTests.Samples.ApplicationInfrastructureDependencies;
using EmergencyHub.ArchitectureTests.Samples.BuildingBlocksApiDependencies;
using EmergencyHub.ArchitectureTests.Samples.DomainDependencies;
using EmergencyHub.ArchitectureTests.Samples.InfrastructureApiDependencies;
using EmergencyHub.ArchitectureTests.Samples.InfrastructureAspNetCoreDependencies;

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
}
