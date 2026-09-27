namespace EmergencyHub.ArchitectureTests;

// 안전장치(S03 계획 결정 BL-085): src의 제품 프로젝트가 ServiceDefaults · AppHost를 빼고 모두 ArchitectureAssemblies.All에 있어야 한다.
// 목록에 빠진 프로젝트는 어떤 규칙에도 걸리지 않아 조용히 검사 밖에 남기 때문이다(목록 한 곳 관리의 약점 보완).
[Trait("FR", "PRD-001/FR-09")]
[Trait("NFR", "PRD-001/NFR-02")]
public sealed class ArchitectureAssemblyCoverageTests
{
    private const string Employee = "EmergencyHub.Employee";

    // ---- 성공 ----

    [Fact]
    public void All_ListsEverySourceProductProjectExceptServiceDefaultsAndAppHost()
    {
        var sourceProjects = ProductProjects.InSourceTree();

        sourceProjects.Should().Contain([$"{Employee}.Api", $"{Employee}.MigrationService", "EmergencyHub.BuildingBlocks.Domain"], "src 트리를 읽지 못하면 공허 통과다");
        ProductProjects.FindUnlisted(sourceProjects, ArchitectureAssemblies.All.Select(assembly => assembly.Name)).Should().BeEmpty(
            "src의 제품 프로젝트는 ArchitectureAssemblies.All에 레이어와 함께 넣고 ArchitectureTests csproj에 참조를 더한다");
    }

    [Fact]
    public void All_EmployeeService_HasFiveLayersEachOnce()
    {
        var employee = ArchitectureAssemblies.All.Where(assembly => assembly.ServiceName == Employee).ToList();

        employee.Select(assembly => (assembly.Name, assembly.Layer)).Should().Equal(
            ($"{Employee}.Domain", ArchitectureLayer.Domain),
            ($"{Employee}.Application", ArchitectureLayer.Application),
            ($"{Employee}.Infrastructure", ArchitectureLayer.Infrastructure),
            ($"{Employee}.Api", ArchitectureLayer.Api),
            ($"{Employee}.MigrationService", ArchitectureLayer.MigrationService));
    }

    // ---- 실패 ----

    [Fact]
    public void FindUnlisted_SourceProjectMissingFromList_ReturnsIt()
    {
        var unlisted = ProductProjects.FindUnlisted(
            ["EmergencyHub.BuildingBlocks.Domain", "EmergencyHub.Notification.Api", "EmergencyHub.Notification.Domain"],
            ["EmergencyHub.BuildingBlocks.Domain", "EmergencyHub.Notification.Domain"]);

        unlisted.Should().Equal("EmergencyHub.Notification.Api");
    }

    // ---- 엣지 ----

    [Fact]
    public void FindUnlisted_ServiceDefaultsAndAppHost_AreExcludedButLookalikesAreNot()
    {
        var unlisted = ProductProjects.FindUnlisted(
            [ProductNames.ServiceDefaults, ProductNames.AppHost, "EmergencyHub.ServiceDefaultsExtras", "EmergencyHub.AppHost.Helpers"],
            []);

        unlisted.Should().Equal("EmergencyHub.AppHost.Helpers", "EmergencyHub.ServiceDefaultsExtras");
    }

    [Fact]
    public void Excluded_IsExactlyServiceDefaultsAndAppHost() =>
        ProductProjects.Excluded.Should().BeEquivalentTo([ProductNames.ServiceDefaults, ProductNames.AppHost]);

    [Fact]
    public void InSourceTree_IgnoresBuildOutputFolders()
    {
        var sourceProjects = ProductProjects.InSourceTree();

        sourceProjects.Should().OnlyHaveUniqueItems("bin · obj 아래 복사본을 세지 않는다");
        sourceProjects.Should().OnlyContain(name => name.StartsWith(ProductNames.Root + ".", StringComparison.Ordinal));
    }
}
