using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>ID를 돌려주는 Command 예시. <see cref="Value"/>가 음수면 검증 실패, 0이면 Handler 실패.</summary>
/// <param name="Value">값.</param>
public sealed record CreateSampleCommand(int Value) : ICommand<Guid>;
