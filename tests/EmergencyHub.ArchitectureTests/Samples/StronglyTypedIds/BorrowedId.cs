using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.ArchitectureTests.Samples.StronglyTypedIds;

/// <summary>위반 예: TSelf가 다른 ID(값 변환기 등록에서 조용히 빠짐, BL-072).</summary>
/// <param name="Value">값.</param>
public readonly record struct BorrowedId(Guid Value) : IStronglyTypedId<OrderId>
{
    /// <inheritdoc />
    public bool Equals(OrderId other) => Value == other.Value;
}
