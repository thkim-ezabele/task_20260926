using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.ArchitectureTests.Samples.ControllerDependencies;

/// <summary>
/// 규칙 대상이 아닌 예: Api의 DI 등록 코드는 Infrastructure를 쓸 수 있다(ADR-0024 <c>&lt;Service&gt;.Api</c> 행 "DI 등록만").
/// Controller가 아니므로 규칙 대상으로 선택되지 않아야 한다.
/// </summary>
public static class SampleApiRegistration
{
    /// <summary>Infrastructure 공통 등록을 호출한다.</summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <returns>같은 서비스 컬렉션.</returns>
    public static IServiceCollection AddSampleApi(this IServiceCollection services) => services.AddBuildingBlocksInfrastructure();
}
