using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;

/// <summary>
/// 직원 조회 응답입니다. Read Repository가 읽기 전용 DbContext에서 바로 프로젝션합니다(도메인 모델을 거치지 않음).
/// </summary>
/// <param name="Id">직원 ID.</param>
/// <param name="DisplayName">표시 이름.</param>
/// <param name="Email">정규화한 이메일.</param>
/// <param name="EmployeeStatus">직원 상태(API에서 정수로 직렬화).</param>
/// <param name="CreatedAt">생성 시각(UTC, 감사 컬럼).</param>
/// <param name="UpdatedAt">마지막 수정 시각(UTC, 감사 컬럼).</param>
public sealed record EmployeeResponse(
    Guid Id,
    string DisplayName,
    string Email,
    EmployeeStatus EmployeeStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
