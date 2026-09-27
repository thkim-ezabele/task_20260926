using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>다른 Aggregate를 ID로만 참조하는 속성(NULL 허용)의 변환 확인용.</remarks>
public readonly record struct CustomerId(Guid Value) : IStronglyTypedId<CustomerId>;
