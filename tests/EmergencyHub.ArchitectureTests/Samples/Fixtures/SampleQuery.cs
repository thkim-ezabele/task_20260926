using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.ArchitectureTests.Samples.Fixtures;

/// <summary>표본용 Query(규칙 범위 밖 공용 픽스처).</summary>
/// <param name="Id">조회할 ID.</param>
public sealed record SampleQuery(Guid Id) : IQuery<Unit>;
