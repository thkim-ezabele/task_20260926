using EmergencyHub.BuildingBlocks.Domain.Entities;
using EmergencyHub.Employee.Domain.Employees.Events;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 Aggregate Root입니다. 팩토리 <see cref="Register"/>로만 만들고 상태는 도메인 메서드로만 바꿉니다.
/// </summary>
/// <remarks>
/// <para>
/// 불변식(이름 1 ~ 100자, 이메일 형식 · 1 ~ 254자, 정의된 상태, 빈 ID 아님)을 어기면 예외를 던집니다(S03 계획 리뷰 결정).
/// 입력 오류는 Application Validator가 먼저 <c>Result</c>(21001 ~ 21006, 1002)로 거르므로, 여기 예외는 프로그래밍 오류입니다.
/// 길이는 정규화 뒤 <see cref="string.Length"/>(UTF-16 코드 단위) 기준입니다.
/// </para>
/// <para>
/// 감사 컬럼(<c>created_at</c> · <c>updated_at</c>)과 동시성 토큰(<c>xmin</c>)은 Infrastructure의 shadow property라 여기 없습니다.
/// </para>
/// </remarks>
public sealed class Employee : AggregateRoot<EmployeeId>
{
    /// <summary>표시 이름 최대 길이(DB <c>varchar(100)</c>).</summary>
    public const int DisplayNameMaxLength = 100;

    /// <summary>이메일 최대 길이(DB <c>varchar(254)</c>).</summary>
    public const int EmailMaxLength = 254;

    private Employee(EmployeeId id, string displayName, string email, EmployeeStatus employeeStatus)
        : base(id)
    {
        DisplayName = displayName;
        Email = email;
        EmployeeStatus = employeeStatus;
    }

    // EF Core 구체화 전용. 도메인 코드는 Register를 쓴다.
    private Employee()
    {
    }

    /// <summary>표시 이름(앞뒤 공백 제거한 값)입니다.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>이메일(앞뒤 공백 제거 + 문화권 무관 소문자, <see cref="EmployeeEmail.Normalize"/>)입니다.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>직원 상태입니다. DB 컬럼 이름이 <c>employee_status</c>가 되도록 속성 이름을 형식 이름과 같게 둡니다.</summary>
    public EmployeeStatus EmployeeStatus { get; private set; }

    /// <summary>
    /// 직원을 등록합니다. 이름과 이메일을 정규화하고 <see cref="EmployeeRegisteredDomainEvent"/>를 수집합니다.
    /// </summary>
    /// <param name="id">직원 ID. Handler가 <c>IIdGenerator</c>로 만듭니다.</param>
    /// <param name="displayName">표시 이름. 앞뒤 공백을 지운 뒤 1 ~ 100자여야 합니다.</param>
    /// <param name="email">이메일. 정규화한 뒤 형식이 맞고 1 ~ 254자여야 합니다.</param>
    /// <param name="status">직원 상태. 정의된 값이어야 하고 0(예약)은 안 됩니다.</param>
    /// <returns>등록한 직원.</returns>
    /// <exception cref="ArgumentException">ID가 비었거나, 이름 · 이메일이 비었거나 형식 · 길이 규칙을 어긴 경우.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="displayName"/> 또는 <paramref name="email"/>이 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/>가 예약 값이거나 정의되지 않은 경우.</exception>
    public static Employee Register(EmployeeId id, string displayName, string email, EmployeeStatus status)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("직원 ID가 비어 있습니다.", nameof(id));
        }

        var employee = new Employee(id, NormalizeDisplayName(displayName), NormalizeEmail(email), EnsureDefined(status));
        employee.Raise(new EmployeeRegisteredDomainEvent(id));

        return employee;
    }

    /// <summary>
    /// 직원을 비활성으로 바꿉니다. 이미 비활성이면 아무것도 바꾸지 않습니다(멱등).
    /// </summary>
    public void Deactivate() => EmployeeStatus = EmployeeStatus.Inactive;

    private static string NormalizeDisplayName(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);

        var trimmed = displayName.Trim();
        if (trimmed.Length is 0 or > DisplayNameMaxLength)
        {
            throw new ArgumentException($"표시 이름은 앞뒤 공백을 지운 뒤 1 ~ {DisplayNameMaxLength}자여야 합니다.", nameof(displayName));
        }

        return trimmed;
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = EmployeeEmail.Normalize(email);
        if (normalized.Length > EmailMaxLength || !EmployeeEmail.IsWellFormed(normalized))
        {
            throw new ArgumentException($"이메일은 형식이 맞고 앞뒤 공백을 지운 뒤 1 ~ {EmailMaxLength}자여야 합니다.", nameof(email));
        }

        return normalized;
    }

    private static EmployeeStatus EnsureDefined(EmployeeStatus status) =>
        status != EmployeeStatus.Unknown && Enum.IsDefined(status)
            ? status
            : throw new ArgumentOutOfRangeException(nameof(status), status, "직원 상태는 정의된 값이어야 합니다(0은 예약 값).");
}
