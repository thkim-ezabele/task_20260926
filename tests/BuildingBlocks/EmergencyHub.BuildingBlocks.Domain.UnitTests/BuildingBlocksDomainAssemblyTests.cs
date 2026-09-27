using System.Reflection;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests;

// 골격 확인용(S01-T05). S01-T06에서 실제 도메인 타입 테스트로 대체한다.
public sealed class BuildingBlocksDomainAssemblyTests
{
    [Fact]
    public void Load_WithAssemblyName_ReturnsBuildingBlocksDomainAssembly()
    {
        var assemblyName = new AssemblyName("EmergencyHub.BuildingBlocks.Domain");

        var assembly = Assembly.Load(assemblyName);

        assembly.GetName().Name.Should().Be("EmergencyHub.BuildingBlocks.Domain");
    }
}
