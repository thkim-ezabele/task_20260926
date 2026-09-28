using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Application.Pipeline;

/// <summary>
/// 로깅 데코레이터의 로그 정의입니다. 이벤트 ID는 공통 범위 중 Mediator 하위 범위 101 ~ 199를 씁니다
/// (원본: wiki/05-api/error-codes.md "로그 이벤트 ID 범위", BL-028).
/// </summary>
/// <remarks>
/// 요청 형식 이름, 결과 코드, <c>ErrorType</c>(정수), 경과 시간만 남깁니다. 요청 · 응답 값과 오류 메시지는 남기지 않습니다(개인정보, ADR-0015).
/// 성공은 <see cref="LogLevel.Debug"/>, 실패 <c>Result</c>는 <see cref="LogLevel.Information"/>입니다(예상 가능한 실패는 Error가 아님).
/// </remarks>
internal static partial class MediatorLogs
{
    [LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = "Command {RequestName} succeeded in {ElapsedMilliseconds} ms")]
    public static partial void CommandSucceeded(this ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Information,
        Message = "Command {RequestName} failed with error {ErrorCode} of type {ErrorType} in {ElapsedMilliseconds} ms")]
    public static partial void CommandFailed(this ILogger logger, string requestName, int errorCode, short errorType, double elapsedMilliseconds);

    [LoggerMessage(EventId = 103, Level = LogLevel.Debug, Message = "Query {RequestName} succeeded in {ElapsedMilliseconds} ms")]
    public static partial void QuerySucceeded(this ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Information,
        Message = "Query {RequestName} failed with error {ErrorCode} of type {ErrorType} in {ElapsedMilliseconds} ms")]
    public static partial void QueryFailed(this ILogger logger, string requestName, int errorCode, short errorType, double elapsedMilliseconds);
}
