using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.ArchitectureTests.Samples.Fixtures;

/// <summary>표본용 강타입 ID(규칙 범위 밖 공용 픽스처).</summary>
/// <param name="Value">값.</param>
public readonly record struct SampleId(Guid Value) : IStronglyTypedId<SampleId>;
