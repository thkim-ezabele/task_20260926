using System.Reflection;

namespace EmergencyHub.Employee.Api;

/// <summary>
/// Employee Api 어셈블리의 참조용 마커입니다. 아키텍처 테스트 대상 목록(<c>ArchitectureAssemblies.All</c>, S03-T04)이 이 형식으로 어셈블리를 가리킵니다.
/// </summary>
public static class EmployeeApiAssembly
{
    /// <summary>Employee Api 어셈블리.</summary>
    public static Assembly Assembly { get; } = typeof(EmployeeApiAssembly).Assembly;
}
