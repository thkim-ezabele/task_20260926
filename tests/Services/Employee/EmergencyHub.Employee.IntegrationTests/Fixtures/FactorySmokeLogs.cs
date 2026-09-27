using Microsoft.Extensions.Logging;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// EmployeeApiFactoryTests가 호스트 ILogger로 한 줄 남겨 수집 싱크 연결을 확인할 때 쓴다.
internal static partial class FactorySmokeLogs
{
    public const string Template = "Factory smoke {Value}";

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = Template)]
    public static partial void FactorySmoke(this ILogger logger, int value);
}
