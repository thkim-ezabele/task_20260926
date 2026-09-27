namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 상태 코드입니다(ADR-0008, DB <c>employee_status smallint</c>). 배포된 값은 바꾸거나 재사용하지 않습니다.
/// </summary>
/// <remarks>
/// 0(<see cref="Unknown"/>)은 코드값 규칙의 예약 값이라 업무 값으로 쓰지 않습니다. Aggregate는 0을 거부하고,
/// 요청 검증은 1002, DB 체크 제약 <c>ck_employees_employee_status</c>는 <c>IN (1, 2)</c>입니다(공통 규칙이 0을 빼고 만듦).
/// </remarks>
public enum EmployeeStatus : short
{
    /// <summary>예약 값. 업무 값으로 쓰지 않습니다.</summary>
    Unknown = 0,

    /// <summary>재직(활성).</summary>
    Active = 1,

    /// <summary>비활성.</summary>
    Inactive = 2,
}
