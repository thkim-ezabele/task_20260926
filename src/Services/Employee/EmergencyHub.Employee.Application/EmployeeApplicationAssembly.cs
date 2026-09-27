using System.Reflection;

namespace EmergencyHub.Employee.Application;

/// <summary>
/// Employee Application 어셈블리의 검색용 마커입니다. <c>AddConventionalServices</c>에 이 어셈블리를 넘겨
/// Handler · Validator를 자동 등록합니다(ADR-0010, ADR-0017).
/// </summary>
public static class EmployeeApplicationAssembly
{
    /// <summary>Employee Application 어셈블리.</summary>
    public static Assembly Assembly { get; } = typeof(EmployeeApplicationAssembly).Assembly;
}
