namespace EmergencyHub.ArchitectureTests;

// 제품 프로젝트 이름에서 BuildingBlocks · 서비스를 구별하는 규칙(서비스 격리 규칙과 선언 참조 규칙의 기준).
[Trait("FR", "PRD-001/FR-09")]
public sealed class ProductNamesTests
{
    [Theory]
    [InlineData("EmergencyHub.Employee.Api", "EmergencyHub.Employee")]
    [InlineData("EmergencyHub.Employee.MigrationService", "EmergencyHub.Employee")]
    [InlineData("EmergencyHub.Employee.Domain.Tests", "EmergencyHub.Employee")]
    [InlineData("EmergencyHub.Employee", "EmergencyHub.Employee")]
    [InlineData("EmergencyHub.AppHostile.Api", "EmergencyHub.AppHostile")]
    public void ServiceOf_ServiceProjectName_ReturnsFirstTwoSegments(string name, string expected) =>
        ProductNames.ServiceOf(name).Should().Be(expected);

    [Theory]
    [InlineData("EmergencyHub.BuildingBlocks.Domain")]
    [InlineData("EmergencyHub.BuildingBlocks")]
    [InlineData("EmergencyHub.ServiceDefaults")]
    [InlineData("EmergencyHub.AppHost")]
    [InlineData("EmergencyHub.AppHost.Tests")]
    [InlineData("EmergencyHub")]
    [InlineData("EmergencyHubTools.Api")]
    [InlineData("Npgsql")]
    public void ServiceOf_SharedOrNonProductName_ReturnsNull(string name) =>
        ProductNames.ServiceOf(name).Should().BeNull();

    [Theory]
    [InlineData("EmergencyHub.BuildingBlocks.Api", AssemblyOwnership.BuildingBlocks)]
    [InlineData("EmergencyHub.Employee.Infrastructure", AssemblyOwnership.Service)]
    [InlineData("EmergencyHub.ServiceDefaults", AssemblyOwnership.Unknown)]
    [InlineData("EmergencyHub.AppHost", AssemblyOwnership.Unknown)]
    [InlineData("Npgsql", AssemblyOwnership.Unknown)]
    public void OwnershipOf_ProjectName_ClassifiesBuildingBlocksServiceOrUnknown(string name, AssemblyOwnership expected) =>
        ProductNames.OwnershipOf(name).Should().Be(expected);

    [Fact]
    public void OwnershipOf_EveryListedAssembly_IsKnown() =>
        ArchitectureAssemblies.All.Should().OnlyContain(
            assembly => assembly.Ownership != AssemblyOwnership.Unknown,
            "검사 대상은 BuildingBlocks 또는 서비스 프로젝트다(ServiceDefaults · AppHost는 목록에 넣지 않는다)");
}
