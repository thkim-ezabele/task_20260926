using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>Handler를 등록하지 않는 Command 예시(누락 시 예외 확인용).</summary>
public sealed record UnregisteredCommand : ICommand<int>;
