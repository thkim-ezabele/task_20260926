using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.ArchitectureTests.Samples.RecordModels;

/// <summary>규칙을 지킨 예: record Command.</summary>
/// <param name="Name">이름.</param>
public sealed record RecordCommand(string Name) : ICommand<Guid>;
