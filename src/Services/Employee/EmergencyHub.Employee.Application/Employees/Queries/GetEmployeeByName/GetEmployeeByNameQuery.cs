using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;

/// <summary>
/// 이름이 정확히 같은 직원 한 명을 조회합니다(PRD-002 FR-08, <c>GET /api/employee/{name}</c>).
/// </summary>
/// <remarks>
/// 앞뒤 공백 제거 + NFC 뒤 정확히 일치(대소문자 구분)하는 이름을 찾고, 동명이인이면 입사일 → ID(등록 순) 첫 1명입니다.
/// 이름 규칙은 <see cref="GetEmployeeByNameQueryValidator"/>가 Name Value Object로 판정합니다(21007 · 21008 · 21009).
/// </remarks>
/// <param name="Name">경로에서 받은 이름(정규화 전 입력 값). 공백만 있는 경로 값은 MVC 단순 형식 바인더가 <see langword="null"/>로 바꿉니다(21007).</param>
public sealed record GetEmployeeByNameQuery(string? Name) : IQuery<EmployeeResponse>;
