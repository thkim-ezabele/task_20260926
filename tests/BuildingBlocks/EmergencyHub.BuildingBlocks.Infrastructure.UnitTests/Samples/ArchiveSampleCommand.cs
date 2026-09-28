using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>반환 값이 없는 Command 예시(한 인자 Handler 편의 인터페이스로 구현).</summary>
/// <param name="Value">값.</param>
public sealed record ArchiveSampleCommand(int Value) : ICommand;
