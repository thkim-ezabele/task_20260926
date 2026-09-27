using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// Guid 생성자가 없는 강타입 ID(규칙 위반 예시). 어떤 엔티티도 이 형식을 쓰지 않으므로 검색되어 변환 규칙이 등록돼도
/// 변환기 인스턴스는 만들어지지 않는다. 변환기 생성 실패는 StronglyTypedIdValueConverterTests가 직접 확인한다.
/// </remarks>
public readonly record struct IdWithoutGuidConstructor : IStronglyTypedId<IdWithoutGuidConstructor>
{
    public Guid Value => Guid.Empty;
}
