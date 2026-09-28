using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>
/// 같은 응답 형식으로 Command와 Query를 함께 구현한 비정상 예시. Command · Query 캐시가 섞이지 않는지 확인한다.
/// </summary>
public sealed record CommandAndQueryRequest : ICommand<int>, IQuery<int>;
