namespace EmergencyHub.Employee.Api.Employees;

/// <summary>
/// 직원 등록 응답 본문 <c>{ "id": "..." }</c>입니다(api-guidelines "생성 Command는 생성한 ID만").
/// </summary>
/// <param name="Id">새 직원 ID(UUID v7).</param>
public sealed record RegisterEmployeeResponse(Guid Id);
