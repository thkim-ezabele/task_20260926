namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;

/// <summary>
/// 일괄 등록 결과입니다(PRD-002 FR-06 성공 응답 <c>{ "count": N, "ids": [...] }</c>).
/// </summary>
/// <param name="Count">등록한 직원 수.</param>
/// <param name="Ids">등록한 직원 ID(입력 순서, 행 번호 오름차순).</param>
public sealed record RegisterEmployeesResponse(int Count, IReadOnlyList<Guid> Ids);
