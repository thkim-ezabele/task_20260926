namespace EmergencyHub.BuildingBlocks.Domain.Events;

/// <summary>
/// 도메인 이벤트를 수집하는 객체(Aggregate Root)의 비제네릭 계약입니다. <c>AggregateRoot&lt;TId&gt;</c>가 구현합니다.
/// </summary>
/// <remarks>
/// UnitOfWork가 커밋에 성공한 뒤 변경 추적기의 엔트리에서 ID 형식 인자를 모르고도 이벤트를 비우도록 둡니다(ADR-0014, database.md "UnitOfWork 커밋 순서").
/// 이벤트를 넣는 멤버는 없습니다. 발생은 Aggregate의 도메인 메서드만 합니다.
/// </remarks>
public interface IHasDomainEvents
{
    /// <summary>발생 순서대로 수집된 도메인 이벤트입니다(읽기 전용).</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>수집한 도메인 이벤트를 모두 비웁니다. 비어 있어도 호출할 수 있습니다.</summary>
    void ClearDomainEvents();
}
