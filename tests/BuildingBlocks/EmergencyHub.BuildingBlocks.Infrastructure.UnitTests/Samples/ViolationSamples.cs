using EmergencyHub.BuildingBlocks.Application.Services;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

// 규칙 위반 시나리오용 인터페이스. 이 어셈블리에는 구현이 없고, 구현은 DynamicSampleAssembly가 테스트마다 만든다.

/// <summary>두 구현이 같은 인터페이스를 등록하는 시나리오(중복 → 시작 실패).</summary>
public interface IDuplicatedSampleService : IService;

/// <summary>한 구현이 서비스 인터페이스 두 개를 구현하는 시나리오의 첫째.</summary>
public interface IFirstPairedSampleService : IService;

/// <summary>한 구현이 서비스 인터페이스 두 개를 구현하는 시나리오의 둘째.</summary>
public interface ISecondPairedSampleService : IService;
