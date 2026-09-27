using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee;

/// <summary>
/// 테스트용 <see cref="RegisterEmployeeCommand"/> 빌더입니다. 테스트와 무관한 값은 기본값에 맡깁니다.
/// </summary>
/// <remarks>예시 이메일은 example.com만 씁니다(S03 인계 메모).</remarks>
internal sealed class RegisterEmployeeCommandBuilder
{
    public const string DefaultDisplayName = "홍길동";

    public const string DefaultEmail = "hong@example.com";

    private string _displayName = DefaultDisplayName;
    private string _email = DefaultEmail;
    private EmployeeStatus? _status = EmployeeStatus.Active;

    public RegisterEmployeeCommandBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public RegisterEmployeeCommandBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public RegisterEmployeeCommandBuilder WithStatus(EmployeeStatus? status)
    {
        _status = status;
        return this;
    }

    public RegisterEmployeeCommand Build() => new(_displayName, _email, _status);
}
