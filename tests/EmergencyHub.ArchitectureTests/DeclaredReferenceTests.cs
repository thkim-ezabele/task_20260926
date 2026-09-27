using EmergencyHub.ArchitectureTests.References;

namespace EmergencyHub.ArchitectureTests;

// 선언 참조 규칙(쓰지 않는 참조도 금지, S02-T05 결정). 원본: ADR-0024 의존성 규칙 표 "참조 금지" 열.
// 형식 의존 규칙(DependencyRuleTests)이 잡지 못하는 "csproj에 적었지만 쓰지 않는 참조"를 deps.json으로 확인한다.
[Trait("FR", "PRD-001/FR-09")]
[Trait("NFR", "PRD-001/NFR-02")]
public sealed class DeclaredReferenceTests
{
    public static TheoryData<string> ProjectNames { get; } = new(ArchitectureAssemblies.All.Select(assembly => assembly.Name));

    // ---- 제품 ----

    [Theory]
    [MemberData(nameof(ProjectNames))]
    public void DeclaredReferences_OfProductProject_ExcludeForbiddenProjectsAndPackages(string projectName)
    {
        var project = ArchitectureAssemblies.All.Single(assembly => assembly.Name == projectName);
        var declared = DeclaredReferences.Of(projectName);

        declared.Should().NotBeNull("{0}이(가) 이 테스트 프로젝트의 의존 그래프(deps.json)에 있어야 한다", projectName);
        DeclaredReferenceRules.FindViolations(project, declared!).Should().BeEmpty(
            "{0}의 선언 참조는 ADR-0024 표의 참조 금지 항목을 포함하지 않는다. 선언 참조: {1}",
            projectName,
            string.Join(", ", declared!));
    }

    // ---- 규칙 동작(선언 참조 목록을 직접 넣어 확인) ----

    [Fact]
    public void FindViolations_DomainDeclaringAnyNonDomainReference_ReturnsThoseReferences()
    {
        var domain = ArchitectureAssemblies.In(ArchitectureLayer.Domain)[0];

        var violations = DeclaredReferenceRules.FindViolations(domain, ["EmergencyHub.BuildingBlocks.Domain", "System.Text.Json", "FluentValidation"]);

        violations.Should().BeEquivalentTo(["System.Text.Json", "FluentValidation"]);
    }

    [Fact]
    public void FindViolations_ApplicationDeclaringUnusedInfrastructureApiAndFrameworks_ReturnsEachForbiddenReference()
    {
        var application = ArchitectureAssemblies.In(ArchitectureLayer.Application)[0];
        string[] declared =
        [
            "EmergencyHub.BuildingBlocks.Domain",
            "FluentValidation",
            "Microsoft.Extensions.Logging.Abstractions",
            "EmergencyHub.BuildingBlocks.Infrastructure",
            "EmergencyHub.BuildingBlocks.Api",
            "Microsoft.EntityFrameworkCore.Relational",
            "EFCore.NamingConventions",
            "Npgsql",
            "Scrutor",
            "Microsoft.AspNetCore.Http.Abstractions",
            "Swashbuckle.AspNetCore",
        ];

        var violations = DeclaredReferenceRules.FindViolations(application, declared);

        violations.Should().BeEquivalentTo(declared[3..]);
    }

    [Fact]
    public void FindViolations_InfrastructureDeclaringApiOrAspNetCore_ReturnsThoseButAllowsDatabaseAndScrutor()
    {
        var infrastructure = ArchitectureAssemblies.In(ArchitectureLayer.Infrastructure)[0];

        var violations = DeclaredReferenceRules.FindViolations(
            infrastructure,
            ["Npgsql.EntityFrameworkCore.PostgreSQL", "Scrutor", "EmergencyHub.BuildingBlocks.Api", "Swashbuckle.AspNetCore", "Microsoft.OpenApi"]);

        violations.Should().BeEquivalentTo(["EmergencyHub.BuildingBlocks.Api", "Swashbuckle.AspNetCore", "Microsoft.OpenApi"]);
    }

    [Fact]
    public void FindViolations_BuildingBlocksApiDeclaringUnusedInfrastructureOrDatabase_ReturnsThoseButAllowsSwashbuckle()
    {
        var api = ArchitectureAssemblies.In(ArchitectureLayer.Api).Single(assembly => assembly.IsBuildingBlocks);

        var violations = DeclaredReferenceRules.FindViolations(
            api,
            ["EmergencyHub.BuildingBlocks.Application", "Swashbuckle.AspNetCore", "EmergencyHub.BuildingBlocks.Infrastructure", "Microsoft.EntityFrameworkCore", "Npgsql"]);

        violations.Should().BeEquivalentTo(["EmergencyHub.BuildingBlocks.Infrastructure", "Microsoft.EntityFrameworkCore", "Npgsql"]);
    }

    // ---- 엣지 ----

    [Fact]
    public void FindViolations_NameSharingPrefixWithoutDotBoundary_IsNotForbidden()
    {
        var application = ArchitectureAssemblies.In(ArchitectureLayer.Application)[0];

        var violations = DeclaredReferenceRules.FindViolations(application, ["NpgsqlLike", "ScrutorExtras", "EFCoreish"]);

        violations.Should().BeEmpty("접두사는 점 단위로 비교한다");
    }

    [Fact]
    public void FindViolations_ServiceApiWithoutDeclaredRestriction_ReturnsEmpty()
    {
        var serviceApi = new LayerAssembly(typeof(DeclaredReferenceTests).Assembly, ArchitectureLayer.Api);

        var violations = DeclaredReferenceRules.FindViolations(serviceApi, ["EmergencyHub.Employee.Infrastructure", "Microsoft.EntityFrameworkCore"]);

        violations.Should().BeEmpty("서비스 Api는 DI 등록에 Infrastructure를 참조할 수 있다(ADR-0024 <Service>.Api 행)");
    }
}
