using EmergencyHub.Employee.Api.Logging;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace EmergencyHub.Employee.Api.UnitTests.Logging;

// logging-observability "헬스체크"(헬스 요청은 요청 로그에서 제외, 판별은 HealthEndpoints.IsHealthPath)와 ADR-0020 "요청 로그".
// 헬스 경로는 Verbose로 낮춰 최소 수준(Information, Development는 Debug)에서 걸러지게 하고, 나머지는 Serilog 기본 판정(5xx · 예외 Error)과 같다.
[Trait("FR", "PRD-001/FR-11")]
public sealed class RequestLogLevelsTests
{
    // ---- 성공 ----

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(409)]
    public void Get_ApiRequestBelow500_ReturnsInformation(int statusCode) =>
        RequestLogLevels.Get(CreateContext("/api/v1/employees", statusCode), null).Should().Be(LogEventLevel.Information);

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health")]
    public void Get_HealthPath_ReturnsVerboseSoItIsFilteredOut(string path) =>
        RequestLogLevels.Get(CreateContext(path, 200), null).Should().Be(LogEventLevel.Verbose);

    // ---- 실패 ----

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    public void Get_ServerErrorStatus_ReturnsError(int statusCode) =>
        RequestLogLevels.Get(CreateContext("/api/v1/employees", statusCode), null).Should().Be(LogEventLevel.Error);

    [Fact]
    public void Get_Exception_ReturnsErrorEvenWith200() =>
        RequestLogLevels.Get(CreateContext("/api/v1/employees", 200), new InvalidOperationException("boom")).Should().Be(LogEventLevel.Error);

    [Fact]
    public void Get_NullContext_ThrowsArgumentNullException()
    {
        var act = () => RequestLogLevels.Get(null!, null);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpContext");
    }

    // ---- 엣지 ----

    [Fact]
    public void Get_UnhealthyReadyCheck503_StaysVerbose() =>
        RequestLogLevels.Get(CreateContext("/health/ready", 503), null).Should().Be(LogEventLevel.Verbose, "헬스 실패 원인은 헬스 검사 로그 · 추적에서 본다");

    [Theory]
    [InlineData("/HEALTH/live", LogEventLevel.Verbose)]
    [InlineData("/healthz", LogEventLevel.Information)]
    [InlineData("/api/v1/health", LogEventLevel.Information)]
    public void Get_PathSegmentsAreComparedCaseInsensitively(string path, LogEventLevel expected) =>
        RequestLogLevels.Get(CreateContext(path, 200), null).Should().Be(expected);

    private static DefaultHttpContext CreateContext(string path, int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.StatusCode = statusCode;
        return context;
    }
}
