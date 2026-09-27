namespace EmergencyHub.BuildingBlocks.Domain.Errors;

/// <summary>
/// 모든 서비스가 쓰는 공통 에러 코드(서비스 자리 0)입니다.
/// 원본은 wiki/05-api/error-codes.md "공통 에러 코드" 표이며, 코드와 표가 일치해야 합니다.
/// </summary>
public static class CommonErrors
{
    /// <summary>1001 · 요청 검증 실패. 필드별 상세는 <see cref="ValidationError"/>에 담습니다.</summary>
    public static readonly Error ValidationFailed = Error.Validation(1001, "요청 값이 올바르지 않습니다.");

    /// <summary>1002 · 정의되지 않은 코드값 / 비트 플래그.</summary>
    public static readonly Error InvalidCode = Error.Validation(1002, "정의되지 않은 코드값입니다.");

    /// <summary>1003 · 페이징 · 정렬 매개변수 오류.</summary>
    public static readonly Error InvalidPaging = Error.Validation(1003, "페이징 또는 정렬 매개변수가 올바르지 않습니다.");

    /// <summary>2001 · 리소스 없음(서비스별 코드가 없을 때).</summary>
    public static readonly Error NotFound = Error.NotFound(2001, "리소스를 찾을 수 없습니다.");

    /// <summary>3001 · 동시 수정 충돌(낙관적 잠금).</summary>
    public static readonly Error ConcurrencyConflict = Error.Conflict(3001, "다른 요청이 먼저 수정했습니다. 다시 시도하세요.");

    /// <summary>3002 · 같은 <c>Idempotency-Key</c>로 이미 처리된 요청.</summary>
    public static readonly Error DuplicateRequest = Error.Conflict(3002, "이미 처리된 요청입니다.");

    /// <summary>
    /// 3003 · 서비스 매핑이 없는 유니크 제약 위반(PostgreSQL 23505). 매핑이 있으면 서비스 에러 코드(예: 23001 이메일 중복)를 씁니다.
    /// 메시지는 고정 문구이며 제약 이름 · 중복 값을 담지 않습니다(database.md "영속성 예외 변환").
    /// </summary>
    public static readonly Error UniqueConstraintViolated = Error.Conflict(3003, "이미 존재하는 값과 중복됩니다.");

    /// <summary>5001 · 인증 필요.</summary>
    public static readonly Error Unauthenticated = Error.Unauthorized(5001, "인증이 필요합니다.");

    /// <summary>5002 · 권한 없음.</summary>
    public static readonly Error Forbidden = Error.Forbidden(5002, "권한이 없습니다.");

    /// <summary>9001 · 예상하지 못한 오류(전역 예외 처리기).</summary>
    public static readonly Error Unexpected = Error.Internal(9001, "예상하지 못한 오류가 발생했습니다.");

    /// <summary>9002 · 외부 시스템 오류.</summary>
    public static readonly Error ExternalServiceFailed = Error.External(9002, "외부 시스템 연동 중 오류가 발생했습니다.");

    /// <summary>9003 · 일시적 장애(재시도 가능).</summary>
    public static readonly Error TemporarilyUnavailable = Error.Unavailable(9003, "일시적으로 처리할 수 없습니다. 잠시 후 다시 시도하세요.");
}
