using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// Employee 서비스 에러 코드(서비스 자리 2)입니다. 원본은 wiki/05-api/error-codes.md "Employee 에러 코드" 표이며 코드와 표가 일치해야 합니다.
/// </summary>
/// <remarks>
/// 메시지는 고정 문구이고 입력 값 · 제약 이름을 담지 않습니다. <see cref="DuplicateEmail"/>은 Handler 사전 검사와
/// Infrastructure의 23505 매핑(<c>ux_employees_normalized_email</c>)이 <b>같은 인스턴스</b>를 씁니다.
/// 폐기 코드(21001 · 21002 · 21006, PRD-001 샘플)는 상수를 지웠고 번호를 재사용하지 않습니다.
/// </remarks>
public static class EmployeeErrors
{
    /// <summary>21003 · 이메일 필수.</summary>
    public static readonly Error EmailRequired = Error.Validation(21003, "이메일은 필수입니다.");

    /// <summary>21004 · 이메일 형식 오류.</summary>
    public static readonly Error EmailInvalid = Error.Validation(21004, "이메일 형식이 올바르지 않습니다.");

    /// <summary>21005 · 이메일 길이 초과(앞뒤 공백 제거 뒤 254자 초과).</summary>
    public static readonly Error EmailTooLong = Error.Validation(21005, "이메일은 254자 이하여야 합니다.");

    /// <summary>21007 · 이름 필수(누락 · 빈 값 · 공백만). 판정 원본은 <see cref="Name.Create"/>.</summary>
    public static readonly Error NameRequired = Error.Validation(21007, "이름은 필수입니다.");

    /// <summary>21008 · 이름 길이 초과(앞뒤 공백 제거 + NFC 뒤 UTF-16 100자 초과). 판정 원본은 <see cref="Name.Create"/>.</summary>
    public static readonly Error NameTooLong = Error.Validation(21008, "이름은 100자 이하여야 합니다.");

    /// <summary>21009 · 이름에 허용하지 않는 문자(제어 문자 또는 짝 없는 서로게이트). 판정 원본은 <see cref="Name.Create"/>.</summary>
    public static readonly Error NameInvalidCharacter = Error.Validation(21009, "이름에 허용하지 않는 문자가 있습니다.");

    /// <summary>21010 · 전화번호 필수(누락 · 빈 값 · 공백만). 판정 원본은 <see cref="PhoneNumber.Create"/>.</summary>
    public static readonly Error PhoneNumberRequired = Error.Validation(21010, "전화번호는 필수입니다.");

    /// <summary>21011 · 전화번호에 ASCII 숫자와 하이픈 밖의 문자가 있음. 판정 원본은 <see cref="PhoneNumber.Create"/>.</summary>
    public static readonly Error PhoneNumberInvalidCharacter = Error.Validation(21011, "전화번호는 숫자와 하이픈만 쓸 수 있습니다.");

    /// <summary>21012 · 전화번호 숫자 자리 수가 8 ~ 15 밖. 판정 원본은 <see cref="PhoneNumber.Create"/>.</summary>
    public static readonly Error PhoneNumberDigitCountOutOfRange = Error.Validation(21012, "전화번호 숫자는 8 ~ 15자리여야 합니다.");

    /// <summary>21013 · 전화번호 전체 길이 20자 초과. 판정 원본은 <see cref="PhoneNumber.Create"/>.</summary>
    public static readonly Error PhoneNumberTooLong = Error.Validation(21013, "전화번호는 20자 이하여야 합니다.");

    /// <summary>21014 · 전화번호 맨 앞 · 맨 뒤 · 연속 하이픈. 판정 원본은 <see cref="PhoneNumber.Create"/>.</summary>
    public static readonly Error PhoneNumberInvalidHyphen = Error.Validation(21014, "전화번호의 하이픈 위치가 올바르지 않습니다.");

    /// <summary>21015 · 입사일 필수(누락 · 빈 값 · 공백만). 판정 원본은 <see cref="JoinedOn.Create"/>.</summary>
    public static readonly Error JoinedOnRequired = Error.Validation(21015, "입사일은 필수입니다.");

    /// <summary>21016 · 입사일이 <c>yyyy-MM-dd</c> 정확 형식 · 있는 날짜가 아님. 판정 원본은 <see cref="JoinedOn.Create"/>.</summary>
    public static readonly Error JoinedOnInvalidFormat = Error.Validation(21016, "입사일은 yyyy-MM-dd 형식의 있는 날짜여야 합니다.");

    /// <summary>21017 · 입사일이 1900-01-01 이전. 판정 원본은 <see cref="JoinedOn.Create"/>.</summary>
    public static readonly Error JoinedOnTooEarly = Error.Validation(21017, "입사일은 1900-01-01 이후여야 합니다.");

    /// <summary>22001 · 직원 없음.</summary>
    public static readonly Error NotFound = Error.NotFound(22001, "직원을 찾을 수 없습니다.");

    /// <summary>23001 · 이메일 중복(<c>normalized_email</c> 기준, 대소문자만 다른 이메일 포함).</summary>
    public static readonly Error DuplicateEmail = Error.Conflict(23001, "이미 등록된 이메일입니다.");
}
