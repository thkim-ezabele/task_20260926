using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>두 번째 Aggregate의 강타입 ID. 공통 규칙이 샘플 하나(Order)에만 맞춰져 있지 않은지 확인한다(S02-T04 인수).</remarks>
public readonly record struct DispatchId(Guid Value) : IStronglyTypedId<DispatchId>;
