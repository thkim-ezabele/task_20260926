using EmergencyHub.BuildingBlocks.Application.Services;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>한 구현이 서비스 인터페이스 두 개를 구현하는 시나리오의 둘째.</summary>
/// <remarks>
/// 규칙 위반 시나리오용 인터페이스입니다. 이 어셈블리에는 구현이 없고, 구현은 <see cref="DynamicSampleAssembly"/>가 테스트마다 만듭니다.
/// </remarks>
public interface ISecondPairedSampleService : IService;
