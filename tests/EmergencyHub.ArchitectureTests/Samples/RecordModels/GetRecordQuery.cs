using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.ArchitectureTests.Samples.RecordModels;

/// <summary>규칙을 지킨 예: record Query.</summary>
/// <param name="Id">ID.</param>
public sealed record GetRecordQuery(Guid Id) : IQuery<SampleResponse>;
