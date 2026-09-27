using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>Query 예시.</summary>
public sealed record GetSampleQuery(int Id) : IQuery<SampleResponse>;
