using EmergencyHub.BuildingBlocks.Application.Services;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>두 구현이 같은 인터페이스를 등록하는 시나리오(중복 → 시작 실패).</summary>
/// <remarks>
/// 규칙 위반 시나리오용 인터페이스입니다. 이 어셈블리에는 구현이 없고, 구현은 <see cref="DynamicSampleAssembly"/>가 테스트마다 만듭니다.
/// </remarks>
public interface IDuplicatedSampleService : IService;
