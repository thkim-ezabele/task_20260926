using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>검색 대상 어셈블리에 Handler가 없는 Command(중복 Handler 시나리오에서 동적 어셈블리가 구현).</summary>
/// <param name="Value">값.</param>
public sealed record OrphanSampleCommand(int Value) : ICommand;
