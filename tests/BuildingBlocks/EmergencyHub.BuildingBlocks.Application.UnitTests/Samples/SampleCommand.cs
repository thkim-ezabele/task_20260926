using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>반환 값이 없는 Command 예시(<c>ICommand : ICommand&lt;Unit&gt;</c>).</summary>
public sealed record SampleCommand(int Value) : ICommand;
