using EmergencyHub.BuildingBlocks.Domain.Entities;
using EmergencyHub.Employee.Domain.Employees.Events;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 Aggregate Root입니다. 팩토리 <see cref="Register"/>로만 만들고 상태는 도메인 메서드로만 바꿉니다.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Register"/>는 <c>Create</c>로 검증된 Value Object만 받습니다(PRD-002 FR-01, ADR-0026 8절). 필드 규칙 위반은 VO <c>Create</c>의
/// Result(21003 ~ 21005, 21007 ~ 21017)가 판정하므로 여기서 다시 검사하지 않고, 빈 ID · <see langword="null"/> VO 같은 불변식 위반만
/// 예외로 막습니다(coding-conventions "실패 처리 경계").
/// </para>
/// <para>
/// 상태는 등록 시 <see cref="EmployeeStatus.Active"/>(1)로 고정하고 입력으로 받지 않습니다(PRD-002 Q13, API 비노출).
/// <see cref="NormalizedEmail"/>은 <see cref="Email.NormalizedEmail"/> 값을 그대로 가지며 유일성 판정 · 유니크 인덱스 대상입니다(ADR-0027).
/// </para>
/// <para>
/// 공개 속성의 선언 순서가 <c>employees</c> 열 순서입니다(database.md 새 스키마 명세). 감사 컬럼(<c>created_at</c> · <c>updated_at</c>)과
/// 동시성 토큰(<c>xmin</c>)은 Infrastructure의 shadow property라 여기 없습니다.
/// </para>
/// </remarks>
public sealed class Employee : AggregateRoot<EmployeeId>
{
    private Employee(EmployeeId id, Name name, Email email, PhoneNumber phoneNumber, JoinedOn joinedOn)
        : base(id)
    {
        Name = name;
        Email = email;
        NormalizedEmail = email.NormalizedEmail;
        PhoneNumber = phoneNumber;
        JoinedOn = joinedOn;
        EmployeeStatus = EmployeeStatus.Active;
    }

    // EF Core 구체화 전용. 도메인 코드는 Register를 쓴다. 값은 EF가 private setter로 채운다.
    private Employee()
    {
        Name = null!;
        Email = null!;
        NormalizedEmail = null!;
        PhoneNumber = null!;
        JoinedOn = null!;
    }

    /// <summary>이름(앞뒤 공백 제거 + NFC, <see cref="Employees.Name.Create"/>)입니다.</summary>
    public Name Name { get; private set; }

    /// <summary>이메일(입력 표기는 <see cref="Email.Value"/>)입니다.</summary>
    public Email Email { get; private set; }

    /// <summary>정규화한 이메일(<see cref="Email.NormalizedEmail"/>, 문화권 무관 소문자)입니다. 이메일 유일성은 이 값으로 판정합니다.</summary>
    public string NormalizedEmail { get; private set; }

    /// <summary>전화번호(입력 그대로)입니다.</summary>
    public PhoneNumber PhoneNumber { get; private set; }

    /// <summary>입사일입니다.</summary>
    public JoinedOn JoinedOn { get; private set; }

    /// <summary>직원 상태입니다. DB 컬럼 이름이 <c>employee_status</c>가 되도록 속성 이름을 형식 이름과 같게 둡니다.</summary>
    public EmployeeStatus EmployeeStatus { get; private set; }

    /// <summary>
    /// 직원을 등록합니다. 상태는 <see cref="EmployeeStatus.Active"/>로 고정하고 <see cref="EmployeeRegisteredDomainEvent"/>를 수집합니다.
    /// </summary>
    /// <param name="id">직원 ID. Handler가 <c>IIdGenerator</c>로 만듭니다.</param>
    /// <param name="name">검증된 이름.</param>
    /// <param name="email">검증된 이메일.</param>
    /// <param name="phoneNumber">검증된 전화번호.</param>
    /// <param name="joinedOn">검증된 입사일.</param>
    /// <returns>등록한 직원.</returns>
    /// <exception cref="ArgumentException">ID가 빈 값인 경우.</exception>
    /// <exception cref="ArgumentNullException">Value Object 중 하나가 <see langword="null"/>인 경우.</exception>
    public static Employee Register(EmployeeId id, Name name, Email email, PhoneNumber phoneNumber, JoinedOn joinedOn)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("직원 ID가 비어 있습니다.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentNullException.ThrowIfNull(joinedOn);

        var employee = new Employee(id, name, email, phoneNumber, joinedOn);
        employee.Raise(new EmployeeRegisteredDomainEvent(id));

        return employee;
    }

    /// <summary>
    /// 직원을 비활성으로 바꿉니다. 이미 비활성이면 아무것도 바꾸지 않습니다(멱등).
    /// </summary>
    public void Deactivate() => EmployeeStatus = EmployeeStatus.Inactive;
}
