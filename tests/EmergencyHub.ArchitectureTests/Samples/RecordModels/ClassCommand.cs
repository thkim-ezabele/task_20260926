using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.ArchitectureTests.Samples.RecordModels;

/// <summary>위반 예: class로 만든 Command(ICommand 구현으로 선택).</summary>
public sealed class ClassCommand : ICommand
{
    /// <summary>이름.</summary>
    public string Name { get; init; } = string.Empty;
}
