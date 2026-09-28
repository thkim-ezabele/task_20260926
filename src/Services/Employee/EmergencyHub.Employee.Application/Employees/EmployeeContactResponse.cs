namespace EmergencyHub.Employee.Application.Employees;

/// <summary>
/// 직원 연락정보 조회 결과입니다. Read Repository가 읽기 DbContext에서 바로 프로젝션합니다(ADR-0007).
/// </summary>
/// <remarks>
/// 목록(PRD-002 FR-07 항목)과 이름 단건(FR-08)이 같은 필드를 쓰므로 두 Query가 함께 씁니다. API 응답 이름(<c>tel</c> · <c>joined</c>)은 S07 Query 응답에서 정합니다.
/// </remarks>
/// <param name="Id">직원 ID.</param>
/// <param name="Name">이름(Trim + NFC 저장 값).</param>
/// <param name="Email">이메일 입력 표기(정규화 값 아님).</param>
/// <param name="PhoneNumber">전화번호(입력 그대로).</param>
/// <param name="JoinedOn">입사일.</param>
public sealed record EmployeeContactResponse(Guid Id, string Name, string Email, string PhoneNumber, DateOnly JoinedOn);
