using EmergencyHub.BuildingBlocks.Domain.Events;

namespace EmergencyHub.BuildingBlocks.Domain.Entities;

/// <summary>
/// Aggregate Root의 기반 클래스입니다. 도메인 이벤트를 <b>수집까지만</b> 합니다.
/// </summary>
/// <typeparam name="TId">강타입 ID.</typeparam>
/// <remarks>
/// <para>상속을 위한 기반 클래스이므로 "클래스는 기본 <c>sealed</c>" 규칙의 예외로 <c>abstract</c>입니다.</para>
/// <para>
/// 수집한 이벤트는 커밋 뒤 UnitOfWork가 비제네릭 <see cref="IHasDomainEvents"/>로 찾아 <see cref="ClearDomainEvents"/>로 비웁니다(ADR-0014).
/// 디스패치는 이후 토픽에서 다룹니다(ADR-0023).
/// </para>
/// </remarks>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// 식별자를 받아 Aggregate를 만듭니다. 파생 클래스의 팩토리 메서드가 호출합니다.
    /// </summary>
    /// <param name="id">Aggregate 식별자. Handler가 <c>IIdGenerator</c>로 만듭니다(ADR-0013).</param>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>
    /// ORM 구체화(materialization)용 생성자입니다.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// 발생 순서대로 수집된 도메인 이벤트입니다. 읽기 전용 뷰라 외부에서 수정할 수 없습니다.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 수집한 도메인 이벤트를 모두 비웁니다. 비어 있어도 호출할 수 있습니다.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// 도메인 이벤트를 발생시켜 수집합니다. 파생 Aggregate의 도메인 메서드가 호출합니다.
    /// </summary>
    /// <param name="domainEvent">발생한 도메인 이벤트.</param>
    /// <exception cref="ArgumentNullException"><paramref name="domainEvent"/>가 <see langword="null"/>인 경우.</exception>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
