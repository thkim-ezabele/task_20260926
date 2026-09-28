using Microsoft.Extensions.Logging;

namespace EmergencyHub.Employee.MigrationService;

/// <summary>
/// Employee MigrationService의 로그 정의입니다. 이벤트 ID는 Employee 범위 중 MigrationService 하위 범위 20901 ~ 20999를 씁니다
/// (원본: wiki/05-api/error-codes.md "Employee 로그 이벤트").
/// </summary>
/// <remarks>
/// 연결 문자열 · 호스트 · 사용자 · 비밀번호는 속성에 넣지 않습니다(logging-observability "개인정보 · 보안").
/// 종료 코드는 정수로 남깁니다(ADR-0008).
/// </remarks>
internal static partial class MigrationServiceLogs
{
    [LoggerMessage(EventId = 20901, Level = LogLevel.Information,
        Message = "Migrations applied for {DbContextType} in {ElapsedMilliseconds} ms")]
    public static partial void MigrationsApplied(this ILogger logger, string dbContextType, double elapsedMilliseconds);

    /// <remarks>
    /// 예외는 작업 최상위인 여기서 한 번만 남깁니다. <c>Include Error Detail</c>을 쓰지 않으므로 <c>PostgresException.Detail</c> 값은 Npgsql이 가립니다.
    /// </remarks>
    [LoggerMessage(EventId = 20902, Level = LogLevel.Error,
        Message = "Migrations failed for {DbContextType} with exception {ExceptionType} and SqlState {SqlState} after {ElapsedMilliseconds} ms, exit code {ExitCode}")]
    public static partial void MigrationsFailed(
        this ILogger logger,
        Exception exception,
        string dbContextType,
        string exceptionType,
        string? sqlState,
        double elapsedMilliseconds,
        int exitCode);
}
