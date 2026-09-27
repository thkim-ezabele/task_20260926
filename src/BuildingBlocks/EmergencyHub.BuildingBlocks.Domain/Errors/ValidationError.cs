namespace EmergencyHub.BuildingBlocks.Domain.Errors;

/// <summary>
/// 요청 검증 실패를 나타내는 오류입니다. 대표 코드는 <c>1001</c>(<see cref="CommonErrors.ValidationFailed"/>)이고,
/// 필드별 상세를 <see cref="Errors"/>에 담습니다(ADR-0018).
/// </summary>
/// <remarks>
/// API는 이 오류를 <c>400</c> <c>ProblemDetails</c>(<c>code</c> 1001, 필드별 <c>errors</c>)로 바꿉니다.
/// 동등성은 대표 오류가 같고 필드별 상세가 <b>같은 순서로</b> 같을 때 성립합니다.
/// </remarks>
public sealed record ValidationError : Error
{
    private ValidationError(FieldError[] errors)
        : base(CommonErrors.ValidationFailed)
    {
        Errors = Array.AsReadOnly(errors);
    }

    /// <summary>
    /// 필드별 상세입니다. 생성 순서를 유지하며, 같은 속성의 상세가 여러 개일 수 있습니다. 읽기 전용입니다.
    /// </summary>
    public IReadOnlyList<FieldError> Errors { get; }

    /// <summary>
    /// 필드별 상세로 검증 실패 오류를 만듭니다. 입력 컬렉션은 복사하므로 이후 변경의 영향을 받지 않습니다.
    /// </summary>
    /// <param name="errors">필드별 상세. 하나 이상이어야 합니다.</param>
    /// <returns>만든 검증 실패 오류.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/>가 비었거나 <see langword="null"/> 원소가 있는 경우.</exception>
    public static ValidationError Create(IEnumerable<FieldError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        FieldError[] copied = [.. errors];
        if (copied.Length == 0)
        {
            throw new ArgumentException("필드별 상세가 하나 이상 있어야 합니다.", nameof(errors));
        }

        if (Array.Exists(copied, error => error is null))
        {
            throw new ArgumentException("필드별 상세에 null 원소가 있습니다.", nameof(errors));
        }

        return new ValidationError(copied);
    }

    /// <inheritdoc/>
    public bool Equals(ValidationError? other) =>
        other is not null && base.Equals(other) && Errors.SequenceEqual(other.Errors);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (var error in Errors)
        {
            hash.Add(error);
        }

        return hash.ToHashCode();
    }
}
