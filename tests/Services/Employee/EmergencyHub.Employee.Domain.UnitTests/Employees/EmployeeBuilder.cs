using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

/// <summary>
/// 테스트용 <see cref="Domain.Employees.Employee"/> 빌더입니다. 테스트와 무관한 값은 기본값에 맡깁니다(testing-strategy "테스트 네이밍과 구조").
/// </summary>
/// <remarks>예시 이메일은 example.com만 씁니다(S03 인계 메모).</remarks>
internal sealed class EmployeeBuilder
{
    public const string DefaultDisplayName = "홍길동";

    public const string DefaultEmail = "hong@example.com";

    public static readonly EmployeeId DefaultId = new(Guid.Parse("0192a1b3-0000-7000-8000-000000000001"));

    private EmployeeId _id = DefaultId;
    private string _displayName = DefaultDisplayName;
    private string _email = DefaultEmail;
    private EmployeeStatus _status = EmployeeStatus.Active;

    public EmployeeBuilder WithId(EmployeeId id)
    {
        _id = id;
        return this;
    }

    public EmployeeBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public EmployeeBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public EmployeeBuilder WithStatus(EmployeeStatus status)
    {
        _status = status;
        return this;
    }

    public Domain.Employees.Employee Build() => Domain.Employees.Employee.Register(_id, _displayName, _email, _status);
}
