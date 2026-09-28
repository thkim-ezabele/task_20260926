using System.Diagnostics.CodeAnalysis;

namespace EmergencyHub.BuildingBlocks.Domain.Errors;

/// <summary>
/// 예상 가능한 실패를 나타내는 오류입니다. 정수 코드(<see cref="Code"/>), 메시지, 유형(<see cref="Type"/>)을 가집니다.
/// </summary>
/// <remarks>
/// <para>
/// 코드는 5자리 <c>S T NNN</c> 체계를 따르고(원본: wiki/05-api/error-codes.md) <b>생성 시점에 검증</b>합니다.
/// 규칙을 어기면 프로그래밍 오류이므로 예외를 던집니다. 오류 정의는 보통 <c>static readonly</c> 필드라
/// 규칙 위반은 타입 초기화에서 바로 드러납니다.
/// </para>
/// <list type="bullet">
/// <item><description>범위: 1001 ~ 99999(<see cref="ArgumentOutOfRangeException"/>)</description></item>
/// <item><description>일련번호(NNN)가 000이면 안 됩니다(<see cref="ArgumentException"/>).</description></item>
/// <item><description>서비스 자리(S) 6 ~ 8은 예비라 쓰지 않습니다(<see cref="ArgumentException"/>).</description></item>
/// <item><description>유형 자리(T)가 <c>(short)Type / 10</c>과 같아야 합니다(<see cref="ArgumentException"/>).</description></item>
/// <item><description>메시지는 비어 있거나 공백만 있으면 안 됩니다(<see cref="ArgumentException"/>).</description></item>
/// </list>
/// <para>
/// "모델은 <c>record</c>, 클래스는 기본 <c>sealed</c>" 규칙의 예외로 <b>non-sealed</b> <c>record</c>입니다.
/// 검증 실패의 필드별 상세를 담는 <see cref="ValidationError"/>(ADR-0018)와 행별 충돌 상세를 담는 <see cref="ConflictError"/>(ADR-0028)가 파생합니다.
/// 값을 받는 생성자는 <c>private protected</c>라 어셈블리 밖에서는 유형별 팩토리로만 새 값을 만듭니다.
/// 다만 파생을 이 어셈블리로 막지는 못합니다. non-sealed <c>record</c>는 컴파일러가 만드는 복사 생성자가
/// <c>protected</c>여야 하므로(더 좁히면 CS8878) 어셈블리 밖에서도 복사 생성자를 불러 파생할 수 있습니다.
/// 이때도 값은 이미 검증된 인스턴스에서 복사되므로 코드 · 메시지 · 유형 불변식은 유지됩니다.
/// 속성은 <c>init</c> 접근자가 없어 <c>with</c> 식으로 값을 바꿔 검증을 우회할 수 없습니다.
/// </para>
/// </remarks>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Error는 PRD-001 FR-04 · ADR-0018이 정한 도메인 용어다. 소비자는 C# 서비스뿐이라 VB 키워드 충돌은 해당 없다.")]
public record Error
{
    private const int MinCode = 1001;
    private const int MaxCode = 99999;

    private protected Error(int code, string message, ErrorType type)
    {
        EnsureValidCode(code, type);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Type = type;
    }

    /// <summary>
    /// 정수 에러 코드(<c>S T NNN</c>)입니다. 클라이언트는 메시지가 아니라 이 코드로 분기합니다.
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// 사람이 읽는 설명입니다. 바뀔 수 있으며 개인정보나 내부 구현을 담지 않습니다.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 오류 유형입니다. HTTP 상태는 이 값으로 정합니다.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>검증 실패(<see cref="ErrorType.Validation"/>, 유형 자리 1) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error Validation(int code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>요청 본문 크기 초과(<see cref="ErrorType.PayloadTooLarge"/>, 유형 자리 1) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error PayloadTooLarge(int code, string message) => new(code, message, ErrorType.PayloadTooLarge);

    /// <summary>지원하지 않는 요청 Content-Type(<see cref="ErrorType.UnsupportedMediaType"/>, 유형 자리 1) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error UnsupportedMediaType(int code, string message) => new(code, message, ErrorType.UnsupportedMediaType);

    /// <summary>대상 없음(<see cref="ErrorType.NotFound"/>, 유형 자리 2) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error NotFound(int code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>충돌(<see cref="ErrorType.Conflict"/>, 유형 자리 3) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error Conflict(int code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>업무 규칙 위반(<see cref="ErrorType.BusinessRule"/>, 유형 자리 4) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error BusinessRule(int code, string message) => new(code, message, ErrorType.BusinessRule);

    /// <summary>인증 필요(<see cref="ErrorType.Unauthorized"/>, 유형 자리 5) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error Unauthorized(int code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>권한 없음(<see cref="ErrorType.Forbidden"/>, 유형 자리 5) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error Forbidden(int code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>내부 오류(<see cref="ErrorType.Internal"/>, 유형 자리 9) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error Internal(int code, string message) => new(code, message, ErrorType.Internal);

    /// <summary>외부 연동 오류(<see cref="ErrorType.External"/>, 유형 자리 9) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error External(int code, string message) => new(code, message, ErrorType.External);

    /// <summary>일시적 장애(<see cref="ErrorType.Unavailable"/>, 유형 자리 9) 오류를 만듭니다.</summary>
    /// <param name="code">에러 코드.</param>
    /// <param name="message">메시지.</param>
    /// <returns>만든 오류.</returns>
    /// <exception cref="ArgumentException">코드 또는 메시지가 규칙을 어긴 경우.</exception>
    public static Error Unavailable(int code, string message) => new(code, message, ErrorType.Unavailable);

    private static void EnsureValidCode(int code, ErrorType type)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(code, MinCode);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(code, MaxCode);

        if (code % 1000 == 0)
        {
            throw new ArgumentException($"에러 코드 {code}의 일련번호(NNN)가 000입니다.", nameof(code));
        }

        var serviceDigit = code / 10000;
        if (serviceDigit is >= 6 and <= 8)
        {
            throw new ArgumentException($"에러 코드 {code}의 서비스 자리({serviceDigit})는 예비 번호입니다.", nameof(code));
        }

        var typeDigit = code / 1000 % 10;
        if (typeDigit != (short)type / 10)
        {
            throw new ArgumentException(
                $"에러 코드 {code}의 유형 자리({typeDigit})가 ErrorType.{type}({(short)type})과 맞지 않습니다.", nameof(code));
        }
    }
}
