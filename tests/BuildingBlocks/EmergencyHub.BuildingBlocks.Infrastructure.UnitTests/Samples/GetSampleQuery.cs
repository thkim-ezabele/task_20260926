using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>Query 예시. <see cref="Value"/>가 음수면 검증 실패.</summary>
/// <param name="Value">값.</param>
public sealed record GetSampleQuery(int Value) : IQuery<string>;
