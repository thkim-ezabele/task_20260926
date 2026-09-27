using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Api.Employees;

/// <summary>
/// 직원 등록 요청 본문입니다. 필수 여부 · 길이 · 형식 · 코드값 검증은 Validator가 합니다(ADR-0016 <c>SuppressImplicitRequired...</c>, ADR-0018).
/// </summary>
/// <param name="DisplayName">표시 이름(원본). 누락 · <see langword="null"/>이면 21001.</param>
/// <param name="Email">이메일(원본). 누락 · <see langword="null"/>이면 21003.</param>
/// <param name="EmployeeStatus">직원 상태(정수). 누락이면 21006, 정의되지 않은 값(0 · 99 등)은 1002.</param>
public sealed record RegisterEmployeeRequest(string? DisplayName, string? Email, EmployeeStatus? EmployeeStatus)
{
    /// <summary>
    /// Command로 바꿉니다. <see langword="null"/> 문자열은 빈 문자열로 넘겨 Validator의 필수 규칙(21001 · 21003)이 판정하게 하고,
    /// 값은 정규화하지 않습니다(정규화는 Aggregate).
    /// </summary>
    /// <returns>등록 Command.</returns>
    internal RegisterEmployeeCommand ToCommand() => new(DisplayName ?? string.Empty, Email ?? string.Empty, EmployeeStatus);
}
