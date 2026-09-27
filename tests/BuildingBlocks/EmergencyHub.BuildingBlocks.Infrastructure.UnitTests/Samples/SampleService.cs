namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary><see cref="ISampleService"/> 구현. 다른 마커 구현을 주입받아 의존 해석까지 확인한다.</summary>
/// <remarks>
/// 이 테스트 어셈블리는 AddConventionalServices의 검색 대상이므로, 여기에는 규칙을 지키는 예시만 둡니다.
/// 규칙을 어기는 구현(중복, 인터페이스 둘, 마커 직접 구현)은 <see cref="DynamicSampleAssembly"/>로 별도 어셈블리에 만듭니다.
/// </remarks>
public sealed class SampleService(ISampleRepository repository) : ISampleService
{
    /// <summary>주입받은 Repository.</summary>
    public ISampleRepository Repository { get; } = repository;
}
