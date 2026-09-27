using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Api;

/// <summary>
/// 공통 API 처리의 로그 정의입니다. 이벤트 ID 1(전역 예외 처리기 전용)과 API 하위 범위 301 ~ 399를 씁니다
/// (원본: wiki/05-api/error-codes.md "로그 이벤트 ID 범위" · "API 로그 이벤트", BL-028).
/// </summary>
/// <remarks>
/// 예외 메시지 · 요청 값 · 경로 · 쿼리 문자열은 남기지 않습니다. 예외는 메시지를 뺀 사본(<see cref="Exceptions.RedactedException"/>)만 넘깁니다
/// (ADR-0024 "응답과 로그에 제약 이름 · SQL · 파라미터 값을 넣지 않는다"). 예상하지 못한 예외(9001)만 <see cref="LogLevel.Error"/>입니다.
/// </remarks>
internal static partial class ApiLogs
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Unhandled exception {ExceptionType} while processing {RequestMethod} request, returned error {ErrorCode}")]
    public static partial void UnhandledException(this ILogger logger, Exception redacted, string exceptionType, string requestMethod, int errorCode);

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Warning,
        Message = "Exception {ExceptionType} classified as error {ErrorCode} of type {ErrorType}")]
    public static partial void ExceptionClassified(this ILogger logger, Exception redacted, string exceptionType, int errorCode, short errorType);

    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Information,
        Message = "Bad HTTP request rejected with status {StatusCode}, returned error {ErrorCode}")]
    public static partial void BadHttpRequestRejected(this ILogger logger, int statusCode, int errorCode);

    [LoggerMessage(
        EventId = 303,
        Level = LogLevel.Information,
        Message = "Request aborted by client with exception {ExceptionType}, returned error {ErrorCode}")]
    public static partial void RequestAborted(this ILogger logger, string exceptionType, int errorCode);

    [LoggerMessage(
        EventId = 304,
        Level = LogLevel.Debug,
        Message = "Request model binding failed for fields {FieldNames}, returned error {ErrorCode}")]
    public static partial void ModelBindingFailed(this ILogger logger, string fieldNames, int errorCode);
}
