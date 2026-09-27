using EmergencyHub.ServiceDefaults;
using Serilog.Events;

namespace EmergencyHub.Employee.Api.Logging;

/// <summary>
/// 요청 로그(<c>UseSerilogRequestLogging</c>) 수준 판정입니다(ADR-0020 "요청 로그", logging-observability "헬스체크").
/// </summary>
internal static class RequestLogLevels
{
    /// <summary>
    /// 헬스 경로(<see cref="HealthEndpoints.IsHealthPath"/>)는 <see cref="LogEventLevel.Verbose"/>로 낮춰 최소 수준에서 걸러지게 하고,
    /// 나머지는 Serilog 기본 판정과 같게 예외 또는 5xx면 <see cref="LogEventLevel.Error"/>, 아니면 <see cref="LogEventLevel.Information"/>입니다.
    /// </summary>
    /// <param name="httpContext">요청 컨텍스트.</param>
    /// <param name="exception">요청 로그 미들웨어까지 올라온 예외. 없으면 <see langword="null"/>.</param>
    /// <returns>로그 수준.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/>가 <see langword="null"/>인 경우.</exception>
    public static LogEventLevel Get(HttpContext httpContext, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (HealthEndpoints.IsHealthPath(httpContext.Request.Path))
        {
            return LogEventLevel.Verbose;
        }

        return exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    }
}
