using EmergencyHub.BuildingBlocks.Application.Services;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>구현이 추상 클래스뿐인 서비스 인터페이스. 추상 클래스는 등록하지 않는다.</summary>
/// <remarks>
/// 이 테스트 어셈블리는 AddConventionalServices의 검색 대상이므로, 여기에는 규칙을 지키는 예시만 둡니다.
/// 규칙을 어기는 구현(중복, 인터페이스 둘, 마커 직접 구현)은 <see cref="DynamicSampleAssembly"/>로 별도 어셈블리에 만듭니다.
/// </remarks>
public interface IAbstractOnlySampleService : IService;
