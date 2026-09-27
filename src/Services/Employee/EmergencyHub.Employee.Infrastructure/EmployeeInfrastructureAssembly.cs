using System.Reflection;

namespace EmergencyHub.Employee.Infrastructure;

/// <summary>
/// Employee Infrastructure 어셈블리의 검색용 마커입니다. <c>AddConventionalServices</c>에 이 어셈블리를 넘겨
/// Repository · Read Repository를 자동 등록합니다(ADR-0010, ADR-0017).
/// </summary>
public static class EmployeeInfrastructureAssembly
{
    /// <summary>Employee Infrastructure 어셈블리.</summary>
    public static Assembly Assembly { get; } = typeof(EmployeeInfrastructureAssembly).Assembly;
}
