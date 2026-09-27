using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// ADR-0008 코드값: smallint + short enum, 값 명시. Active = 1, Inactive = 2이고 0은 예약 값(업무 값으로 쓰지 않음).
// DB 체크 제약(ck_employees_employee_status IN (1, 2))은 공통 규칙이 이 정의에서 0을 빼고 만든다(S03 계획 리뷰 employees 확정안).
public sealed class EmployeeStatusTests
{
    [Fact]
    public void UnderlyingType_IsInt16()
    {
        Enum.GetUnderlyingType(typeof(EmployeeStatus)).Should().Be<short>();
    }

    [Fact]
    public void Members_AreReservedZeroActiveOneInactiveTwo()
    {
        Enum.GetValues<EmployeeStatus>().Select(value => ((short)value, value.ToString())).Should().Equal(
            ((short)0, "Unknown"),
            ((short)1, "Active"),
            ((short)2, "Inactive"));
    }

    [Fact]
    public void Type_IsNotFlags()
    {
        typeof(EmployeeStatus).IsDefined(typeof(FlagsAttribute), inherit: false).Should().BeFalse();
    }
}
