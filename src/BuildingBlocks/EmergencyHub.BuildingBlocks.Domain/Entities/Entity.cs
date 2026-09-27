using System.Runtime.CompilerServices;

namespace EmergencyHub.BuildingBlocks.Domain.Entities;

/// <summary>
/// 식별자(<see cref="Id"/>)로 구분되는 도메인 엔터티의 기반 클래스입니다.
/// </summary>
/// <typeparam name="TId">강타입 ID. <c>public readonly record struct EmployeeId(Guid Value);</c> 형태를 권장합니다.</typeparam>
/// <remarks>
/// <para>
/// 상속을 위한 기반 클래스이므로 "클래스는 기본 <c>sealed</c>" 규칙의 예외로 <c>abstract</c>입니다.
/// 파생 Entity / Aggregate는 <c>sealed</c>로 둡니다.
/// </para>
/// <para>
/// 동등성: <b>런타임 타입이 같고 <see cref="Id"/>가 같으면</b> 같은 엔터티입니다.
/// <see cref="Id"/>가 기본값(<c>default(TId)</c>)이면 아직 식별되지 않은 엔터티로 보고 같은 인스턴스일 때만 같습니다.
/// ID는 Handler가 <c>IIdGenerator</c>로 만들어 팩토리에 넘기므로(ADR-0013) 정상 흐름에서 기본값 ID는 생기지 않습니다.
/// </para>
/// </remarks>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// 식별자를 받아 엔터티를 만듭니다. 파생 클래스의 팩토리 메서드가 호출합니다.
    /// </summary>
    /// <param name="id">엔터티 식별자.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// ORM 구체화(materialization)용 생성자입니다. 도메인 코드에서는 <see cref="Entity{TId}(TId)"/>를 씁니다.
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>
    /// 엔터티 식별자입니다. 생성 뒤에는 바뀌지 않습니다.
    /// </summary>
    public TId Id { get; private init; }

    /// <summary>
    /// 두 엔터티가 같은지 비교합니다. <see langword="null"/>은 <see langword="null"/>끼리만 같습니다.
    /// </summary>
    /// <param name="left">왼쪽 피연산자.</param>
    /// <param name="right">오른쪽 피연산자.</param>
    /// <returns>같으면 <see langword="true"/>.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>
    /// 두 엔터티가 다른지 비교합니다.
    /// </summary>
    /// <param name="left">왼쪽 피연산자.</param>
    /// <param name="right">오른쪽 피연산자.</param>
    /// <returns>다르면 <see langword="true"/>.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other.GetType() != GetType() || IsTransient() || other.IsTransient())
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        IsTransient() ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(GetType(), Id);

    private bool IsTransient() => Id.Equals(default);
}
