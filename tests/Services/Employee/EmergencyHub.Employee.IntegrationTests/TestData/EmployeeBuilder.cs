using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.IntegrationTests.TestData;

/// <summary>
/// 테스트용 <see cref="Domain.Employees.Employee"/> 빌더입니다(testing-strategy.md "테스트 네이밍과 구조").
/// </summary>
/// <remarks>
/// <para>
/// 기본값은 매번 다른 ID와 순번이 붙은 고유 이메일(example.com), 규칙에 맞는 이름 · 전화번호 · 입사일, Active입니다(S05-T04 인계 메모).
/// 필드 값은 Value Object <c>Create</c>를 거치므로(<c>normalized_email</c>도 Email VO가 만듦) 규칙을 어긴 값을 넣으면 <see cref="Build"/>에서 예외가 납니다.
/// </para>
/// <para>
/// 상태는 등록 입력이 아니므로(Active 고정) <see cref="EmployeeStatus.Inactive"/>를 고르면 등록 뒤 <see cref="Domain.Employees.Employee.Deactivate"/>를 부릅니다.
/// </para>
/// </remarks>
public sealed class EmployeeBuilder
{
    /// <summary>기본 이름입니다.</summary>
    public const string DefaultName = "Test Employee";

    /// <summary>기본 전화번호입니다.</summary>
    public const string DefaultPhoneNumber = "010-1234-5678";

    /// <summary>기본 입사일(<c>yyyy-MM-dd</c>)입니다.</summary>
    public const string DefaultJoinedOn = "2020-03-02";

    private static long _sequence;

    private EmployeeId _id = new(Guid.NewGuid());
    private string _name = DefaultName;
    private string _email = $"employee-{Interlocked.Increment(ref _sequence)}-{Guid.NewGuid():N}@example.com";
    private string _phoneNumber = DefaultPhoneNumber;
    private string _joinedOn = DefaultJoinedOn;
    private EmployeeStatus _status = EmployeeStatus.Active;

    /// <summary>ID를 정합니다.</summary>
    /// <param name="id">직원 ID.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithId(EmployeeId id)
    {
        _id = id;
        return this;
    }

    /// <summary>이름을 정합니다(Name VO가 Trim + NFC).</summary>
    /// <param name="name">이름 입력 값.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>이메일을 정합니다(Email VO가 Trim, 정규화 값은 <c>ToLowerInvariant</c>).</summary>
    /// <param name="email">이메일 입력 값.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>전화번호를 정합니다(입력 그대로).</summary>
    /// <param name="phoneNumber">전화번호 입력 값.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    /// <summary>입사일을 정합니다(<c>yyyy-MM-dd</c>).</summary>
    /// <param name="joinedOn">입사일 입력 값.</param>
    /// <returns>같은 빌더.</returns>
    public EmployeeBuilder WithJoinedOn(string joinedOn)
    {
        _joinedOn = joinedOn;
        return this;
    }

    /// <summary>등록 뒤 상태를 정합니다. <see cref="EmployeeStatus.Inactive"/>면 <c>Deactivate</c>를 부릅니다.</summary>
    /// <param name="status"><see cref="EmployeeStatus.Active"/> 또는 <see cref="EmployeeStatus.Inactive"/>.</param>
    /// <returns>같은 빌더.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Active · Inactive 밖의 값인 경우(도메인이 만들 수 없는 상태는 원시 SQL로 넣는다).</exception>
    public EmployeeBuilder WithStatus(EmployeeStatus status)
    {
        if (status is not (EmployeeStatus.Active or EmployeeStatus.Inactive))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "빌더는 Active · Inactive만 만들 수 있습니다.");
        }

        _status = status;
        return this;
    }

    /// <summary>도메인 팩터리(<see cref="Domain.Employees.Employee.Register"/>)로 만듭니다.</summary>
    /// <returns>새 직원(등록 이벤트 수집됨).</returns>
    public Domain.Employees.Employee Build()
    {
        var employee = Domain.Employees.Employee.Register(
            _id,
            Name.Create(_name).Value,
            Email.Create(_email).Value,
            PhoneNumber.Create(_phoneNumber).Value,
            JoinedOn.Create(_joinedOn).Value);

        if (_status == EmployeeStatus.Inactive)
        {
            employee.Deactivate();
        }

        return employee;
    }
}
