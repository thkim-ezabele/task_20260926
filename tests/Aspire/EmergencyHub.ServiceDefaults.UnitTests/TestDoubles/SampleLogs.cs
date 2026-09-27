using Microsoft.Extensions.Logging;

namespace EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;

// 테스트에서 남기는 로그(CA1848: LoggerExtensions 대신 [LoggerMessage]). 이벤트 ID는 제품 범위 밖의 테스트 전용 값이다.
internal static partial class SampleLogs
{
    public const string RegisteredTemplate = "Employee {EmployeeId} registered";

    [LoggerMessage(EventId = 9_000_001, Level = LogLevel.Information, Message = RegisteredTemplate)]
    public static partial void Registered(this ILogger logger, int employeeId);

    [LoggerMessage(EventId = 9_000_002, Level = LogLevel.Information, Message = "Information sample")]
    public static partial void InformationSample(this ILogger logger);

    [LoggerMessage(EventId = 9_000_003, Level = LogLevel.Warning, Message = "Warning sample")]
    public static partial void WarningSample(this ILogger logger);

    [LoggerMessage(EventId = 9_000_004, Level = LogLevel.Error, Message = "Error sample")]
    public static partial void ErrorSample(this ILogger logger, Exception? exception);

    [LoggerMessage(EventId = 9_000_005, Level = LogLevel.Critical, Message = "Critical sample")]
    public static partial void CriticalSample(this ILogger logger);
}
