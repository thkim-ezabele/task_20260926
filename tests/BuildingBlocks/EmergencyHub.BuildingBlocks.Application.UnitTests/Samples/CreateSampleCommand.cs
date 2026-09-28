using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>생성한 ID를 돌려주는 Command 예시.</summary>
public sealed record CreateSampleCommand(string Name) : ICommand<Guid>;
