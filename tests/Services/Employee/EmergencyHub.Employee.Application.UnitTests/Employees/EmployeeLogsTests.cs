using System.Reflection;
using EmergencyHub.Employee.Application.Employees;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.Employee.Application.UnitTests.Employees;

// 로그 이벤트 ID: Employee 범위 20001 ~ 20999. 원본은 wiki/05-api/error-codes.md "Employee 로그 이벤트".
public sealed class EmployeeLogsTests
{
    private static readonly IReadOnlyList<(MethodInfo Method, LoggerMessageAttribute Definition)> Definitions = typeof(EmployeeLogs)
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Select(method => (method, method.GetCustomAttribute<LoggerMessageAttribute>()))
        .Where(pair => pair.Item2 is not null)
        .Select(pair => (pair.method, pair.Item2!))
        .ToList();

    [Fact]
    public void Definitions_AreTheDocumentedEvents()
    {
        Definitions.Select(pair => (pair.Definition.EventId, pair.Definition.Level, pair.Definition.Message)).Should().Equal(
            (20001, LogLevel.Information, "Employee {EmployeeId} registered"));
    }

    [Fact]
    public void EventIds_AreInsideEmployeeRange()
    {
        Definitions.Should().NotBeEmpty();
        Definitions.Should().OnlyContain(pair => pair.Definition.EventId >= 20001 && pair.Definition.EventId <= 20999);
    }

    [Fact]
    public void Parameters_CarryNoPersonalDataTypesOrNames()
    {
        // 개인정보(이름 · 이메일)를 남기지 않는다(logging-observability "개인정보 · 보안"). 매개변수는 로거 + 식별자만.
        var parameters = Definitions.SelectMany(pair => pair.Method.GetParameters().Skip(1)).ToList();

        parameters.Should().OnlyContain(parameter => parameter.ParameterType == typeof(Guid));
        parameters.Should().NotContain(parameter =>
            parameter.Name!.Contains("email", StringComparison.OrdinalIgnoreCase)
            || parameter.Name.Contains("name", StringComparison.OrdinalIgnoreCase));
    }
}
