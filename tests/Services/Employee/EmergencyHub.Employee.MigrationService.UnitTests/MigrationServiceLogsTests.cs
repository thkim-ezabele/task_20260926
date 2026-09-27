using System.Reflection;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.Employee.MigrationService.UnitTests;

// 로그 이벤트 ID: Employee 범위 20001 ~ 20999 중 MigrationService 하위 범위 20901 ~ 20999. 원본은 wiki/05-api/error-codes.md "Employee 로그 이벤트".
public sealed class MigrationServiceLogsTests
{
    private static readonly IReadOnlyList<(MethodInfo Method, LoggerMessageAttribute Definition)> Definitions = typeof(MigrationServiceLogs)
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Select(method => (method, method.GetCustomAttribute<LoggerMessageAttribute>()))
        .Where(pair => pair.Item2 is not null)
        .Select(pair => (pair.method, pair.Item2!))
        .ToList();

    [Fact]
    public void Definitions_AreTheDocumentedEvents()
    {
        Definitions.Select(pair => (pair.Definition.EventId, pair.Definition.Level, pair.Definition.Message)).Should().Equal(
            (20901, LogLevel.Information, "Migrations applied for {DbContextType} in {ElapsedMilliseconds} ms"),
            (20902, LogLevel.Error,
                "Migrations failed for {DbContextType} with exception {ExceptionType} and SqlState {SqlState} after {ElapsedMilliseconds} ms, exit code {ExitCode}"));
    }

    [Fact]
    public void EventIds_AreInsideMigrationServiceSubRange()
    {
        Definitions.Should().NotBeEmpty();
        Definitions.Should().OnlyContain(pair => pair.Definition.EventId >= 20901 && pair.Definition.EventId <= 20999);
    }

    [Fact]
    public void Parameters_CarryNoConnectionInformation()
    {
        var names = Definitions.SelectMany(pair => pair.Method.GetParameters().Skip(1)).Select(parameter => parameter.Name!).ToList();

        names.Should().NotContain(name =>
            name.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || name.Contains("host", StringComparison.OrdinalIgnoreCase)
            || name.Contains("user", StringComparison.OrdinalIgnoreCase)
            || name.Contains("password", StringComparison.OrdinalIgnoreCase));
    }
}
