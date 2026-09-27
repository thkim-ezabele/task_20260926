using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.ArchitectureTests.Samples.StronglyTypedIds;

/// <summary>규칙을 지킨 예: TSelf가 자기 자신.</summary>
/// <param name="Value">값.</param>
public readonly record struct OrderId(Guid Value) : IStronglyTypedId<OrderId>;
