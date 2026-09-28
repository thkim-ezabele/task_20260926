namespace EmergencyHub.BuildingBlocks.Domain.Errors;

/// <summary>
/// 검증 실패의 필드별 상세입니다: 속성 경로, 정수 코드, 메시지(ADR-0018).
/// </summary>
/// <remarks>
/// <see cref="Create"/>로만 만들고, 코드는 검증 실패 유형의 <see cref="Error"/>에서 가져옵니다.
/// 속성은 <c>init</c> 접근자가 없어 <c>with</c> 식으로 검증을 우회할 수 없습니다.
/// </remarks>
public sealed record FieldError
{
    private FieldError(string propertyName, int code, string message)
    {
        PropertyName = propertyName;
        Code = code;
        Message = message;
    }

    /// <summary>
    /// 속성 경로입니다. FluentValidation의 <c>PropertyName</c>(예: <c>Email</c>, <c>Items[0].Name</c>)을 그대로 담고,
    /// JSON 키(camelCase) 변환은 API 계층이 합니다. 객체 수준 규칙은 빈 문자열입니다.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// 규칙별 정수 에러 코드입니다. 유형은 항상 <see cref="ErrorType.Validation"/>입니다.
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// 사람이 읽는 설명입니다.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 속성 경로와 검증 실패 오류로 필드별 상세를 만듭니다.
    /// </summary>
    /// <param name="propertyName">속성 경로. 객체 수준 규칙이면 빈 문자열.</param>
    /// <param name="error">검증 실패 유형(<see cref="ErrorType.Validation"/>)의 오류.</param>
    /// <returns>만든 필드별 상세.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> 또는 <paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException">속성 경로가 공백만 있거나, 오류 유형이 검증 실패가 아닌 경우.</exception>
    public static FieldError Create(string propertyName, Error error)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(error);

        if (propertyName.Length > 0 && string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("속성 경로는 공백만으로 이루어질 수 없습니다.", nameof(propertyName));
        }

        if (error.Type != ErrorType.Validation)
        {
            throw new ArgumentException(
                $"필드별 상세에는 검증 실패 유형의 오류만 담습니다. 받은 유형: ErrorType.{error.Type}", nameof(error));
        }

        return new FieldError(propertyName, error.Code, error.Message);
    }
}
