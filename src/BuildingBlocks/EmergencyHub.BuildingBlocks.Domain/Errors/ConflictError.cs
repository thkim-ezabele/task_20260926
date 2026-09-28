namespace EmergencyHub.BuildingBlocks.Domain.Errors;

/// <summary>
/// 항목별 상세를 가진 충돌 오류입니다(ADR-0028). 대표 오류는 호출한 쪽이 준 충돌 유형 <see cref="Error"/>(예: 이메일 중복 <c>23001</c>)이고,
/// 항목별 상세(예: 충돌한 행 경로)를 <see cref="Details"/>에 담습니다.
/// </summary>
/// <remarks>
/// <para>
/// API는 이 오류를 <c>409</c> <c>ProblemDetails</c>(<c>code</c>는 대표 코드, <see cref="ValidationError"/>와 같은 모양의 <c>errors</c>)로 바꿉니다.
/// 상세가 없는 충돌(<c>3001</c> · <c>3003</c> 등)은 이 형식을 쓰지 않고 일반 <see cref="Error"/>로 둡니다(<c>errors</c> 없음).
/// </para>
/// <para>동등성은 대표 오류가 같고 항목별 상세가 <b>같은 순서로</b> 같을 때 성립합니다(<see cref="ValidationError"/>와 같은 규칙).</para>
/// </remarks>
public sealed record ConflictError : Error
{
    private ConflictError(Error error, ConflictDetail[] details)
        : base(error)
    {
        Details = Array.AsReadOnly(details);
    }

    /// <summary>
    /// 항목별 상세입니다. 생성 순서를 유지하며, 같은 경로의 상세가 여러 개일 수 있습니다. 읽기 전용입니다.
    /// </summary>
    public IReadOnlyList<ConflictDetail> Details { get; }

    /// <summary>
    /// 대표 충돌 오류와 항목별 상세로 충돌 오류를 만듭니다. 입력 컬렉션은 복사하므로 이후 변경의 영향을 받지 않습니다.
    /// </summary>
    /// <param name="error">대표 오류. 충돌 유형(<see cref="ErrorType.Conflict"/>)이어야 합니다. 코드 · 메시지 · 유형을 그대로 씁니다.</param>
    /// <param name="details">항목별 상세. 하나 이상이어야 합니다.</param>
    /// <returns>만든 충돌 오류.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> 또는 <paramref name="details"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="error"/>가 충돌 유형이 아니거나, <paramref name="details"/>가 비었거나 <see langword="null"/> 원소가 있는 경우.
    /// </exception>
    public static ConflictError Create(Error error, IEnumerable<ConflictDetail> details)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(details);

        if (error.Type != ErrorType.Conflict)
        {
            throw new ArgumentException(
                $"대표 오류는 충돌 유형이어야 합니다. 받은 유형: ErrorType.{error.Type}", nameof(error));
        }

        ConflictDetail[] copied = [.. details];
        if (copied.Length == 0)
        {
            throw new ArgumentException("충돌 상세가 하나 이상 있어야 합니다.", nameof(details));
        }

        if (Array.Exists(copied, detail => detail is null))
        {
            throw new ArgumentException("충돌 상세에 null 원소가 있습니다.", nameof(details));
        }

        return new ConflictError(error, copied);
    }

    /// <inheritdoc/>
    public bool Equals(ConflictError? other) =>
        other is not null && base.Equals(other) && Details.SequenceEqual(other.Details);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (var detail in Details)
        {
            hash.Add(detail);
        }

        return hash.ToHashCode();
    }
}
