using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.IntegrationTests.TestData;

/// <summary>
/// 테스트용 <see cref="Domain.Employees.Employee"/> 빌더입니다(testing-strategy.md "테스트 네이밍과 구조"). 기본값은 매번 다른 ID · 이메일(example.com)과 Active입니다.
/// </summary>
public sealed class EmployeeBuilder
{
    private EmployeeId _id = new(Guid.NewGuid());
    private string _displayName = "Test Employee";
    private string _email = $"employee-{Guid.NewGuid():N}@example.com";
    private EmployeeStatus _status = EmployeeStatus.Active;

    /// <summary>ID를 정합니다.</summary>
    /// <param name="id">직원 ID.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithId(EmployeeId id)
    {
        _id = id;
        return this;
    }

    /// <summary>표시 이름을 정합니다.</summary>
    /// <param name="displayName">표시 이름.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    /// <summary>이메일을 정합니다(도메인이 정규화).</summary>
    /// <param name="email">이메일.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>상태를 정합니다.</summary>
    /// <param name="status">직원 상태.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithStatus(EmployeeStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>도메인 팩터리(<see cref="Domain.Employees.Employee.Register"/>)로 만듭니다.</summary>
    /// <returns>새 직원(등록 이벤트 수집됨).</returns>
    public Domain.Employees.Employee Build() => Domain.Employees.Employee.Register(_id, _displayName, _email, _status);
}
