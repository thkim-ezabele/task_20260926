using System.Globalization;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 입사일 Value Object입니다. 필드 규칙의 판정 원본은 <see cref="Create"/>입니다(ADR-0026 8절, PRD-002 FR-01).
/// </summary>
/// <remarks>
/// 판정 순서(첫 실패 하나만 돌려줌): 비었거나 공백만이면 21015 → <see cref="Format"/> 정확 파싱
/// (<see cref="CultureInfo.InvariantCulture"/>, <see cref="DateTimeStyles.None"/>: 앞뒤 공백 · 한 자리 월일 · 없는 날짜 거부)에 실패하면 21016 →
/// <see cref="MinValue"/> 이전이면 21017. 미래 날짜는 허용하므로 현재 시각을 보지 않습니다.
/// </remarks>
public sealed record JoinedOn
{
    /// <summary>입력 형식(정확 일치).</summary>
    public const string Format = "yyyy-MM-dd";

    /// <summary>허용하는 가장 이른 입사일(1900-01-01, 포함).</summary>
    public static readonly DateOnly MinValue = new(1900, 1, 1);

    private JoinedOn(DateOnly value)
    {
        Value = value;
    }

    /// <summary>입사일입니다(DB <c>joined_on date</c>).</summary>
    public DateOnly Value { get; }

    /// <summary>
    /// 입력 문자열로 입사일을 만듭니다.
    /// </summary>
    /// <param name="value">입력 값. <see langword="null"/>이면 필수 오류입니다.</param>
    /// <returns>성공이면 <see cref="JoinedOn"/>, 실패면 21015 · 21016 · 21017 중 하나.</returns>
    public static Result<JoinedOn> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmployeeErrors.JoinedOnRequired;
        }

        if (!DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return EmployeeErrors.JoinedOnInvalidFormat;
        }

        if (date < MinValue)
        {
            return EmployeeErrors.JoinedOnTooEarly;
        }

        return new JoinedOn(date);
    }
}
