using System.Diagnostics.CodeAnalysis;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary><see cref="IInternalSampleService"/>의 internal 구현.</summary>
/// <remarks>
/// 이 테스트 어셈블리는 AddConventionalServices의 검색 대상이므로, 여기에는 규칙을 지키는 예시만 둡니다.
/// 규칙을 어기는 구현(중복, 인터페이스 둘, 마커 직접 구현)은 <see cref="DynamicSampleAssembly"/>로 별도 어셈블리에 만듭니다.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 internal 구현도 검색해 DI가 만든다(테스트 대상 동작).")]
internal sealed class InternalSampleService : IInternalSampleService;
