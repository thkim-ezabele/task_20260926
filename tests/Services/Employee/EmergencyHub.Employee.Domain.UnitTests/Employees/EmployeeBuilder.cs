using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

/// <summary>
/// 테스트용 <see cref="Domain.Employees.Employee"/> 빌더입니다. 테스트와 무관한 값은 기본값에 맡깁니다(testing-strategy "테스트 네이밍과 구조").
/// </summary>
/// <remarks>
/// 필드 값은 입력 문자열로 받고 <see cref="Build"/>에서 Value Object <c>Create</c>를 거칩니다(규칙을 어긴 값이면 <c>Value</c> 접근 예외).
/// 기본 이메일은 빌더마다 순번을 붙여 고유합니다(S05-T04 인계 메모). 예시 이메일은 example.com만 씁니다.
/// </remarks>
internal sealed class EmployeeBuilder
{
    public const string DefaultName = "홍길동";

    public const string DefaultPhoneNumber = "010-1234-5678";

    public const string DefaultJoinedOn = "2020-03-02";

    public static readonly EmployeeId DefaultId = new(Guid.Parse("0192a1b3-0000-7000-8000-000000000001"));

    private static int _sequence;

    private EmployeeId _id = DefaultId;
    private string _name = DefaultName;
    private string _email = $"employee{Interlocked.Increment(ref _sequence)}@example.com";
    private string _phoneNumber = DefaultPhoneNumber;
    private string _joinedOn = DefaultJoinedOn;

    /// <summary>이 빌더가 쓸 이메일 입력 값(기본값은 순번이 붙은 고유 값)입니다.</summary>
    public string EmailInput => _email;

    public EmployeeBuilder WithId(EmployeeId id)
    {
        _id = id;
        return this;
    }

    public EmployeeBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public EmployeeBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public EmployeeBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public EmployeeBuilder WithJoinedOn(string joinedOn)
    {
        _joinedOn = joinedOn;
        return this;
    }

    public Domain.Employees.Employee Build() => Domain.Employees.Employee.Register(
        _id,
        Name.Create(_name).Value,
        Email.Create(_email).Value,
        PhoneNumber.Create(_phoneNumber).Value,
        JoinedOn.Create(_joinedOn).Value);
}
