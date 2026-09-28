using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 전화번호 Value Object입니다. 필드 규칙의 판정 원본은 <see cref="Create"/>입니다(ADR-0026 8절, PRD-002 FR-01).
/// </summary>
/// <remarks>
/// <para>
/// 입력을 그대로 보존합니다(앞뒤 공백 제거 · 하이픈 정규화 없음). 공백은 허용 문자가 아니므로 앞뒤 공백이 있으면 21011입니다.
/// 전화번호 중복은 허용합니다.
/// </para>
/// <para>
/// 판정 순서(첫 실패 하나만 돌려줌): 비었거나 공백만이면 21010 → ASCII 숫자(<see cref="char.IsAsciiDigit(char)"/>)와 <c>-</c> 밖의 문자
/// (<c>+</c> · 공백 · 전각 숫자 포함)가 있으면 21011 → 전체 길이 <see cref="MaxLength"/> 초과면 21013 →
/// 맨 앞 · 맨 뒤 · 연속 하이픈이면 21014 → 숫자 자리 수가 <see cref="MinDigitCount"/> ~ <see cref="MaxDigitCount"/> 밖이면 21012.
/// </para>
/// </remarks>
public sealed record PhoneNumber
{
    /// <summary>전체 최대 길이(숫자 + 하이픈, DB <c>varchar(20)</c>).</summary>
    public const int MaxLength = 20;

    /// <summary>숫자 최소 자리 수.</summary>
    public const int MinDigitCount = 8;

    /// <summary>숫자 최대 자리 수.</summary>
    public const int MaxDigitCount = 15;

    private const char Hyphen = '-';

    private PhoneNumber(string value)
    {
        Value = value;
    }

    /// <summary>입력 그대로의 전화번호입니다.</summary>
    public string Value { get; }

    /// <summary>
    /// 입력 문자열로 전화번호를 만듭니다.
    /// </summary>
    /// <param name="value">입력 값. <see langword="null"/>이면 필수 오류입니다.</param>
    /// <returns>성공이면 <see cref="PhoneNumber"/>, 실패면 21010 ~ 21014 중 하나.</returns>
    public static Result<PhoneNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmployeeErrors.PhoneNumberRequired;
        }

        if (!value.All(character => char.IsAsciiDigit(character) || character == Hyphen))
        {
            return EmployeeErrors.PhoneNumberInvalidCharacter;
        }

        if (value.Length > MaxLength)
        {
            return EmployeeErrors.PhoneNumberTooLong;
        }

        if (value[0] == Hyphen || value[^1] == Hyphen || value.Contains("--", StringComparison.Ordinal))
        {
            return EmployeeErrors.PhoneNumberInvalidHyphen;
        }

        var digitCount = value.Count(char.IsAsciiDigit);
        if (digitCount is < MinDigitCount or > MaxDigitCount)
        {
            return EmployeeErrors.PhoneNumberDigitCountOutOfRange;
        }

        return new PhoneNumber(value);
    }
}
