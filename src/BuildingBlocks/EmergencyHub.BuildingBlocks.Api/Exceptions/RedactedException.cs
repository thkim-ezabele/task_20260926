using System.Diagnostics.CodeAnalysis;

namespace EmergencyHub.BuildingBlocks.Api.Exceptions;

/// <summary>
/// 로그에 넘길 예외 사본입니다. 원본의 <b>형식 이름과 스택 트레이스는 남기고 메시지는 뺍니다</b>(내부 예외 사슬도 같게).
/// </summary>
/// <remarks>
/// <para>
/// 변환되지 않은 DB 예외(23514 · 25006 등)의 메시지에는 제약 이름 · 테이블 이름 · SQL · 값이 들어 있습니다. BuildingBlocks.Api는
/// Npgsql 형식을 모르므로(ADR-0024) 형식으로 골라 낼 수 없어, 모든 예외의 메시지를 빼고 기록합니다
/// (ADR-0024 "응답과 로그에 제약 이름 · SQL · 파라미터 값을 넣지 않는다", ADR-0020). 원인 추적은 형식 이름 · 스택 · 추적 ID로 합니다.
/// </para>
/// <para>
/// <see cref="Exception.ToString"/>은 <see cref="StackTrace"/> 속성을 쓰므로 Serilog 콘솔 · 파일 · OTLP 출력에도 원본 스택이 나옵니다.
/// <see cref="AggregateException"/>은 첫 내부 예외만 따라갑니다. 사슬은 <see cref="MaxDepth"/>단계까지만 복사합니다.
/// </para>
/// </remarks>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "원본 예외에서만 만드는 로그 전용 사본이다(From). 메시지를 받는 표준 생성자는 이 형식의 목적(메시지 제거)과 맞지 않는다.")]
[SuppressMessage(
    "Design",
    "CA1064:Exceptions should be public",
    Justification = "던지지 않고 로그 속성으로만 넘기는 내부 사본이라 공개할 소비자가 없다.")]
internal sealed class RedactedException : Exception
{
    /// <summary>복사하는 예외 사슬의 최대 단계 수입니다(자기 자신 포함).</summary>
    public const int MaxDepth = 16;

    private readonly string? _stackTrace;

    private RedactedException(string originalTypeName, string? stackTrace, RedactedException? inner)
        : base($"Exception of type {originalTypeName} (message redacted)", inner)
    {
        OriginalTypeName = originalTypeName;
        _stackTrace = stackTrace;
    }

    /// <summary>원본 예외 형식의 전체 이름입니다.</summary>
    public string OriginalTypeName { get; }

    /// <summary>원본 예외의 스택 트레이스입니다. 던진 적 없는 예외면 <see langword="null"/>입니다.</summary>
    public override string? StackTrace => _stackTrace;

    /// <summary>원본 예외에서 메시지를 뺀 사본을 만듭니다.</summary>
    /// <param name="exception">원본 예외.</param>
    /// <returns>사본.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/>이 <see langword="null"/>인 경우.</exception>
    public static RedactedException From(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return Copy(exception, depth: 1);
    }

    private static RedactedException Copy(Exception exception, int depth)
    {
        var inner = exception.InnerException is { } next && depth < MaxDepth ? Copy(next, depth + 1) : null;
        return new RedactedException(TypeName(exception), exception.StackTrace, inner);
    }

    private static string TypeName(Exception exception) => exception.GetType().FullName ?? exception.GetType().Name;
}
