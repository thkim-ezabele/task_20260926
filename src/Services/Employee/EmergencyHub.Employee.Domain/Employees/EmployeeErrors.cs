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

    /// <summary>21004 · 이메일 형식 오류(제어 문자 · 짝 없는 서로게이트 포함). 판정 원본은 <see cref="Email.Create"/>.</summary>
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

    /// <summary>21018 · 같은 요청 안 이메일 중복(<see cref="Email.NormalizedEmail"/> 서수 비교, 관련 행 모두 표시). 판정 원본은 일괄 등록 Handler.</summary>
    public static readonly Error DuplicateEmailInRequest = Error.Validation(21018, "같은 요청 안에 중복된 이메일이 있습니다.");

    /// <summary>21019 · CSV 행의 열 개수가 4가 아님(행 오류). 판정 원본은 Application CSV 파서.</summary>
    public static readonly Error CsvColumnCountMismatch = Error.Validation(21019, "CSV 행의 열 개수는 4개여야 합니다.");

    /// <summary>21020 · CSV 닫히지 않은 따옴표(행 오류, 레코드가 시작한 줄). 판정 원본은 Application CSV 파서.</summary>
    public static readonly Error CsvUnclosedQuote = Error.Validation(21020, "CSV 따옴표가 닫히지 않았습니다.");

    /// <summary>21021 · CSV 따옴표 없는 필드 안의 <c>"</c> 또는 닫는 따옴표 뒤의 문자(행 오류). 판정 원본은 Application CSV 파서.</summary>
    public static readonly Error CsvUnexpectedQuote = Error.Validation(21021, "CSV 따옴표 위치가 올바르지 않습니다.");

    /// <summary>21022 · 입력이 올바른 UTF-8이 아님(CP949 · UTF-8로 인코딩한 서로게이트 등, 요청 전체 오류). 판정 원본은 Application 해독 단계.</summary>
    public static readonly Error ImportInvalidUtf8 = Error.Validation(21022, "입력이 올바른 UTF-8이 아닙니다.");

    /// <summary>21023 · JSON 문법 오류(끝 쉼표 · 주석 · <c>[..],[..]</c> · 최대 깊이 64 초과 등, 요청 전체 오류). 판정 원본은 Application JSON 파서.</summary>
    public static readonly Error JsonSyntaxInvalid = Error.Validation(21023, "JSON 문법이 올바르지 않습니다.");

    /// <summary>21024 · JSON 항목이 객체가 아님(<c>null</c> · 숫자 등, 항목 오류). 판정 원본은 Application JSON 파서.</summary>
    public static readonly Error JsonItemNotObject = Error.Validation(21024, "JSON 항목은 객체여야 합니다.");

    /// <summary>21025 · JSON 항목의 필드 값이 문자열이 아님(<c>"joined": 20000101</c> · <c>null</c> 등, 항목 오류). 판정 원본은 Application JSON 파서.</summary>
    public static readonly Error JsonValueNotString = Error.Validation(21025, "JSON 필드 값은 문자열이어야 합니다.");

    /// <summary>21026 · JSON 항목에 같은 필드 속성이 두 번 이상 있음(대소문자만 다른 이름 포함, 항목 오류). 판정 원본은 Application JSON 파서.</summary>
    public static readonly Error JsonDuplicateProperty = Error.Validation(21026, "JSON 항목에 중복된 속성이 있습니다.");

    /// <summary>21027 · 행 수가 1,000을 넘음(요청 전체 오류). 판정 원본은 Application CSV · JSON 파서.</summary>
    public static readonly Error ImportTooManyRows = Error.Validation(21027, "한 번에 1,000행까지 등록할 수 있습니다.");

    /// <summary>21028 · 빈 입력(입력 출처 없음, 길이 0, BOM이나 공백만 있음, 요청 전체 오류 경로 ""). 판정 원본은 일괄 등록 Validator.</summary>
    public static readonly Error ImportInputEmpty = Error.Validation(21028, "등록할 입력이 비어 있습니다.");

    /// <summary>21029 · 입력 출처가 둘 이상(multipart <c>file</c>과 <c>data</c> 동시 전송 등, 요청 전체 오류 경로 ""). 판정 원본은 일괄 등록 Validator.</summary>
    public static readonly Error ImportMultipleSources = Error.Validation(21029, "입력은 한 가지 방식으로만 보낼 수 있습니다.");

    /// <summary>21030 · 400 행 오류가 100개를 넘어 잘림(101번째 항목, 경로 ""). 판정 원본은 일괄 등록 Handler.</summary>
    public static readonly Error RowErrorsTruncated = Error.Validation(21030, "행 오류가 많아 일부만 표시합니다.");

    /// <summary>22001 · 직원 없음.</summary>
    public static readonly Error NotFound = Error.NotFound(22001, "직원을 찾을 수 없습니다.");

    /// <summary>23001 · 이메일 중복(<c>normalized_email</c> 기준, 대소문자만 다른 이메일 포함).</summary>
    public static readonly Error DuplicateEmail = Error.Conflict(23001, "이미 등록된 이메일입니다.");

    /// <summary>23002 · 409 행 충돌이 100개를 넘어 잘림(101번째 항목, 경로 ""). 판정 원본은 일괄 등록 Handler.</summary>
    public static readonly Error RowConflictsTruncated = Error.Conflict(23002, "행 충돌이 많아 일부만 표시합니다.");
}
