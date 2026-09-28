using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// Employee 서비스 에러 코드(서비스 자리 2)입니다. 원본은 wiki/05-api/error-codes.md "Employee 에러 코드" 표이며 코드와 표가 일치해야 합니다.
/// </summary>
/// <remarks>
/// 메시지는 고정 문구이고 입력 값 · 제약 이름을 담지 않습니다. <see cref="DuplicateEmail"/>은 Handler 사전 검사와
/// Infrastructure의 23505 매핑(<c>ux_employees_email</c>)이 <b>같은 인스턴스</b>를 씁니다.
/// </remarks>
public static class EmployeeErrors
{
    /// <summary>21001 · 표시 이름 필수.</summary>
    public static readonly Error DisplayNameRequired = Error.Validation(21001, "표시 이름은 필수입니다.");

    /// <summary>21002 · 표시 이름 길이 초과(앞뒤 공백 제거 뒤 100자 초과).</summary>
    public static readonly Error DisplayNameTooLong = Error.Validation(21002, "표시 이름은 100자 이하여야 합니다.");

    /// <summary>21003 · 이메일 필수.</summary>
    public static readonly Error EmailRequired = Error.Validation(21003, "이메일은 필수입니다.");

    /// <summary>21004 · 이메일 형식 오류.</summary>
    public static readonly Error EmailInvalid = Error.Validation(21004, "이메일 형식이 올바르지 않습니다.");

    /// <summary>21005 · 이메일 길이 초과(앞뒤 공백 제거 뒤 254자 초과).</summary>
    public static readonly Error EmailTooLong = Error.Validation(21005, "이메일은 254자 이하여야 합니다.");

    /// <summary>21006 · 직원 상태 필수. 정의되지 않은 값(0 · 99 등)은 공통 1002입니다.</summary>
    public static readonly Error EmployeeStatusRequired = Error.Validation(21006, "직원 상태는 필수입니다.");

    /// <summary>22001 · 직원 없음.</summary>
    public static readonly Error NotFound = Error.NotFound(22001, "직원을 찾을 수 없습니다.");

    /// <summary>23001 · 이메일 중복(정규화한 이메일 기준).</summary>
    public static readonly Error DuplicateEmail = Error.Conflict(23001, "이미 등록된 이메일입니다.");
}
