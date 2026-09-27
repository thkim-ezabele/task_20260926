namespace EmergencyHub.BuildingBlocks.Domain.Identifiers;

/// <summary>
/// <see cref="Guid"/>를 감싸는 강타입 ID 계약입니다. <c>public readonly record struct EmployeeId(Guid Value) : IStronglyTypedId&lt;EmployeeId&gt;;</c>로 구현합니다.
/// </summary>
/// <typeparam name="TSelf">구현 형식 자신.</typeparam>
/// <remarks>
/// <para>
/// BuildingBlocks.Infrastructure의 EF Core 공통 규칙이 이 인터페이스를 구현한 형식마다 <c>uuid</c> 값 변환기를 등록하고,
/// 키는 <c>ValueGeneratedNever</c>로 둡니다(TD-015, database.md "EF Core 공통 모델 규칙"). 엔티티마다 <c>HasConversion</c>을 쓰지 않습니다.
/// </para>
/// <para>
/// 변환기는 구현 형식의 <see cref="Guid"/> 한 개짜리 public 생성자로 값을 만듭니다(위치 기반 record struct면 자동으로 생깁니다).
/// ID 값은 Handler가 <c>IIdGenerator</c>로 만듭니다(ADR-0013).
/// </para>
/// </remarks>
public interface IStronglyTypedId<TSelf> : IEquatable<TSelf>
    where TSelf : struct, IStronglyTypedId<TSelf>
{
    /// <summary>
    /// 감싼 UUID 값입니다.
    /// </summary>
    Guid Value { get; }
}
