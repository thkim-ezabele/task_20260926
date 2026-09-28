using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 이메일 Value Object입니다. 필드 규칙의 판정 원본은 <see cref="Create"/>입니다(ADR-0026 8절, ADR-0027, PRD-002 FR-01).
/// </summary>
/// <remarks>
/// <para>
/// 판정 순서(첫 실패 하나만 돌려줌): 앞뒤 공백 제거 → 비었으면 21003 → 길이 <see cref="MaxLength"/> 초과면 21005 →
/// 형식이 틀리면 21004. 형식: 제어 문자(Cc, 탭 · DEL 포함) · 짝 없는 서로게이트 없음(<see cref="Name"/>과 같은 판정, BL-129),
/// <c>@</c>가 정확히 하나이고 앞뒤가 비지 않음, 공백(<see cref="char.IsWhiteSpace(char)"/>) 없음,
/// domain에 <c>.</c>가 있고 첫 · 끝 <c>.</c>과 연속 <c>.</c>이 없음(<c>a@.com</c> · <c>a@com.</c> · <c>a@b..c</c> 거부).
/// local 부분에는 <c>.</c> 규칙을 적용하지 않습니다.
/// </para>
/// <para>
/// <see cref="Value"/>는 앞뒤 공백만 뺀 입력 표기이고, <see cref="NormalizedEmail"/>은 <c>Value.ToLowerInvariant()</c>입니다(NFC 정규화 없음).
/// <see cref="string.ToLowerInvariant"/>는 UTF-16 코드 단위를 1:1로 바꾸므로 길이가 같고, 터키어 'İ'(U+0130)는 바꾸지 않습니다.
/// 이메일 유일성은 <see cref="NormalizedEmail"/>로 판정합니다(대소문자만 다른 두 입력은 같은 값).
/// </para>
/// </remarks>
public sealed record Email
{
    /// <summary>최대 길이(앞뒤 공백 제거 뒤 UTF-16 코드 단위, DB <c>varchar(254)</c>).</summary>
    public const int MaxLength = 254;

    private Email(string value)
    {
        Value = value;
        NormalizedEmail = value.ToLowerInvariant();
    }

    /// <summary>앞뒤 공백만 뺀 입력 표기입니다(DB <c>email</c>).</summary>
    public string Value { get; }

    /// <summary>문화권과 무관한 소문자로 바꾼 값입니다(DB <c>normalized_email</c>, 유니크 인덱스 대상).</summary>
    public string NormalizedEmail { get; }

    /// <summary>
    /// 입력 문자열로 이메일을 만듭니다.
    /// </summary>
    /// <param name="value">입력 값. <see langword="null"/>이면 필수 오류입니다.</param>
    /// <returns>성공이면 <see cref="Email"/>, 실패면 21003 · 21004 · 21005 중 하나.</returns>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmployeeErrors.EmailRequired;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            return EmployeeErrors.EmailTooLong;
        }

        if (!IsWellFormed(trimmed))
        {
            return EmployeeErrors.EmailInvalid;
        }

        return new Email(trimmed);
    }

    private static bool IsWellFormed(string value)
    {
        if (EmployeeTextRules.ContainsControlOrUnpairedSurrogate(value))
        {
            return false;
        }

        var at = value.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at == value.Length - 1 || at != value.LastIndexOf('@'))
        {
            return false;
        }

        if (value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var domain = value.AsSpan(at + 1);

        return domain.Contains('.')
            && domain[0] != '.'
            && domain[^1] != '.'
            && !domain.Contains("..", StringComparison.Ordinal);
    }
}
