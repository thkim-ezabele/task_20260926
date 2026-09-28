using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.ArchitectureTests.Samples.Fixtures;

/// <summary>표본용 Command(규칙 범위 밖 공용 픽스처).</summary>
/// <param name="Name">이름.</param>
public sealed record SampleCommand(string Name) : ICommand;
