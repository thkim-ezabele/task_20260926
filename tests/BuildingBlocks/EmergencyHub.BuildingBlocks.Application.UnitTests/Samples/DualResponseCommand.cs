using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>
/// 두 응답 형식의 Command를 함께 구현한 비정상 예시. 캐시 키가 요청 형식만이면 두 번째 호출에서 형 변환 예외가 난다.
/// </summary>
public sealed record DualResponseCommand : ICommand<int>, ICommand<string>;
