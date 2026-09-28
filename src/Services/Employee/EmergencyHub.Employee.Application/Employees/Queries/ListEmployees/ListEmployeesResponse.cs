namespace EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;

/// <summary>
/// 직원 목록 한 쪽입니다(PRD-002 FR-07 응답 <c>{ "items": [...], "totalCount": N, "page": p, "pageSize": s }</c>, ADR-0025).
/// </summary>
/// <param name="Items">이 쪽의 직원(입사일 → ID 순). 마지막 쪽을 넘으면 비어 있습니다.</param>
/// <param name="TotalCount">전체 직원 수.</param>
/// <param name="Page">요청한 쪽 번호.</param>
/// <param name="PageSize">요청한 쪽 크기.</param>
public sealed record ListEmployeesResponse(IReadOnlyList<EmployeeResponse> Items, int TotalCount, int Page, int PageSize);
