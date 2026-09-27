namespace EmergencyHub.BuildingBlocks.Domain.Errors;

/// <summary>
/// 오류 유형입니다. 값은 2자리이고 <b>값 / 10이 에러 코드의 유형 자리(T)</b>입니다.
/// HTTP 상태는 이 유형으로 정합니다(원본: wiki/05-api/error-codes.md).
/// </summary>
/// <remarks>
/// 유형 자리 5(인증 / 권한)와 9(내부 / 외부 연동)는 HTTP 상태가 둘 이상이라 1의 자리로 구분합니다.
/// 배포된 값은 바꾸거나 재사용하지 않습니다.
/// </remarks>
public enum ErrorType : short
{
    /// <summary>예약 값. <see cref="Error"/>에는 쓰지 않습니다.</summary>
    None = 0,

    /// <summary>검증 실패(T = 1, HTTP 400).</summary>
    Validation = 10,

    /// <summary>대상 없음(T = 2, HTTP 404).</summary>
    NotFound = 20,

    /// <summary>충돌(T = 3, HTTP 409).</summary>
    Conflict = 30,

    /// <summary>업무 규칙 위반(T = 4, HTTP 422).</summary>
    BusinessRule = 40,

    /// <summary>인증 필요(T = 5, HTTP 401).</summary>
    Unauthorized = 51,

    /// <summary>권한 없음(T = 5, HTTP 403).</summary>
    Forbidden = 52,

    /// <summary>내부 오류(T = 9, HTTP 500).</summary>
    Internal = 91,

    /// <summary>외부 연동 오류(T = 9, HTTP 502).</summary>
    External = 92,

    /// <summary>일시적 장애, 재시도 가능(T = 9, HTTP 503).</summary>
    Unavailable = 93,
}
