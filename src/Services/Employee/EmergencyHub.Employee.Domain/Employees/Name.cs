using System.Text;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 이름 Value Object입니다. 필드 규칙의 판정 원본은 <see cref="Create"/>입니다(ADR-0026 8절, PRD-002 FR-01).
/// </summary>
/// <remarks>
/// <para>
/// 판정 순서(첫 실패 하나만 돌려줌): 앞뒤 공백 제거(<see cref="string.Trim()"/>) → 비었으면 21007 →
/// 제어 문자(<see cref="char.IsControl(char)"/>, Cc, 탭 포함) 또는 짝 없는 서로게이트면 21009 → NFC 정규화 →
/// UTF-16 길이 <see cref="MaxLength"/> 초과면 21008.
/// </para>
/// <para>
/// 서식 문자(Cf, ZWJ 등)는 허용합니다. 짝 없는 서로게이트는 <see cref="string.Normalize(NormalizationForm)"/>가 예외를 던지므로 정규화 전에 검사합니다.
/// </para>
/// </remarks>
public sealed record Name
{
    /// <summary>최대 길이(NFC 뒤 UTF-16 코드 단위, DB <c>varchar(100)</c>).</summary>
    public const int MaxLength = 100;

    private Name(string value)
    {
        Value = value;
    }

    /// <summary>앞뒤 공백을 지우고 NFC로 정규화한 이름입니다.</summary>
    public string Value { get; }

    /// <summary>
    /// 입력 문자열로 이름을 만듭니다.
    /// </summary>
    /// <param name="value">입력 값. <see langword="null"/>이면 필수 오류입니다.</param>
    /// <returns>성공이면 <see cref="Name"/>, 실패면 21007 · 21008 · 21009 중 하나.</returns>
    public static Result<Name> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmployeeErrors.NameRequired;
        }

        var trimmed = value.Trim();
        if (EmployeeTextRules.ContainsControlOrUnpairedSurrogate(trimmed))
        {
            return EmployeeErrors.NameInvalidCharacter;
        }

        var normalized = trimmed.Normalize(NormalizationForm.FormC);
        if (normalized.Length > MaxLength)
        {
            return EmployeeErrors.NameTooLong;
        }

        return new Name(normalized);
    }
}
