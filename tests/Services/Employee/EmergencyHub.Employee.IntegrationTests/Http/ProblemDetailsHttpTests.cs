using System.Net;
using System.Net.Http.Json;
using System.Text;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S03-T07 HTTP 인수(FR-07 "실패 Result, 바인딩 오류, 정의되지 않은 enum 입력, 처리되지 않은 예외가 API 설계 · 에러 코드 형식(code 포함)으로 응답된다").
// 변환 규칙 자체는 BuildingBlocks.Api 단위 테스트가 본다. 여기서는 실제 Api 파이프라인(모델 바인딩 · Validator · 전역 예외 처리 · 요청 로그)을 거친
// 응답 본문이 employee-api.md 실패 응답 표 · 예시와 같은지(BL-083 H1 TestServer 부분 · H3)만 확인한다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-07")]
public sealed class ProblemDetailsHttpTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string EmployeesPath = "/api/v1/employees";
    private const string ValidationFailed = "요청 값이 올바르지 않습니다.";
    private const string Unexpected = "예상하지 못한 오류가 발생했습니다.";
    private const string SecretMessage = "SELECT secret_column FROM employees WHERE email = 'leak@example.com' -- ux_employees_email";

    // ---- 성공 ----

    [Fact]
    public async Task Post_TraceparentHeader_ProblemTraceIdEqualsIncomingTraceId()
    {
        const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(EmployeesPath, UriKind.Relative))
        {
            Content = JsonContent.Create(new { displayName = "Trace", email = "trace@example.com" }),
        };
        request.Headers.Add("traceparent", $"00-{TraceId}-00f067aa0ba902b7-01");

        using var response = await client.SendAsync(request, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, ValidationFailed, EmployeesPath, CancellationToken);
        problem.GetProperty("traceId").GetString().Should().Be(TraceId, "traceId는 들어온 traceparent의 trace-id를 잇는다(H3)");
        factory.Logs.RequestCompletions.Should().ContainSingle().Which.TraceId.ToString().Should().Be(TraceId, "요청 로그도 같은 추적 ID");
    }

    // ---- 실패 ----

    [Fact]
    public async Task Post_MalformedJson_Returns400With1001AndFieldCode1001()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(new Uri(EmployeesPath, UriKind.Relative), JsonBody("{\"displayName\":"), CancellationToken);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, ValidationFailed, EmployeesPath, CancellationToken);
        problem.FieldCodes().Should().ContainSingle().Which.Code.Should().Be(1001);
    }

    [Fact]
    public async Task Post_EmployeeStatusAsString_Returns400With1001UnderEmployeeStatusKey()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            new Uri(EmployeesPath, UriKind.Relative),
            JsonBody("{\"displayName\":\"Str\",\"email\":\"str@example.com\",\"employeeStatus\":\"Active\"}"),
            CancellationToken);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, ValidationFailed, EmployeesPath, CancellationToken);
        problem.FieldCodes().Should().Equal([("employeeStatus", 1001)], "정수 enum에 문자열은 형식 불일치(ADR-0008, 문자열 변환기 없음)");
        var body = problem.GetRawText();
        body.Should().NotContain("Active").And.NotContain("str@example.com").And.NotContain("System.", "입력 값 · 내부 형식 이름을 담지 않는다");
    }

    [Fact]
    public async Task Get_NonGuidId_Returns400With1001UnderIdKeyInsteadOf404()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri($"{EmployeesPath}/not-a-guid", UriKind.Relative), CancellationToken);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, ValidationFailed, $"{EmployeesPath}/not-a-guid", CancellationToken);
        problem.FieldCodes().Should().Equal([("id", 1001)]);
        problem.GetRawText().Should().NotContain("not-a-guid\"}", "필드 메시지는 고정 문구다");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task Post_UndefinedEmployeeStatus_Returns400With1001AndField1002(int employeeStatus)
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(EmployeesPath, new { displayName = "Code", email = "code@example.com", employeeStatus }, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, ValidationFailed, EmployeesPath, CancellationToken);
        problem.FieldCodes().Should().Equal([("employeeStatus", 1002)]);
        problem.GetProperty("errors").GetProperty("employeeStatus")[0].GetProperty("message").GetString().Should().Be("정의되지 않은 코드값입니다.");
    }

    [Theory]
    [InlineData("{\"displayName\":\"Missing\",\"email\":\"missing@example.com\"}")]
    [InlineData("{\"displayName\":\"Null\",\"email\":\"null@example.com\",\"employeeStatus\":null}")]
    public async Task Post_MissingOrNullEmployeeStatus_Returns400With21006(string json)
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(new Uri(EmployeesPath, UriKind.Relative), JsonBody(json), CancellationToken);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, ValidationFailed, EmployeesPath, CancellationToken);
        problem.FieldCodes().Should().Equal([("employeeStatus", 21006)]);
    }

    [Fact]
    public async Task Post_EmptyBody_Returns400With21001And21003And21006InFieldOrder()
    {
        // 문서 예시처럼 여러 필드 실패는 한 응답의 errors에 선언 순서로 담긴다(암묵적 필수 검사 끔 → Validator가 판정).
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(new Uri(EmployeesPath, UriKind.Relative), JsonBody("{}"), CancellationToken);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, ValidationFailed, EmployeesPath, CancellationToken);
        problem.FieldCodes().Should().Equal(
            ("displayName", 21001),
            ("email", 21003),
            ("employeeStatus", 21006));
    }

    [Fact]
    public async Task Get_UnhandledExceptionInReadRepository_Returns500With9001WithoutOriginalMessageAndLogsOnceAtError()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            ConfigureServices = services =>
            {
                services.RemoveAll<IEmployeeReadRepository>();
                services.AddScoped<IEmployeeReadRepository, ThrowingEmployeeReadRepository>();
            },
        });
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        var id = Guid.NewGuid();

        using var response = await client.GetAsync(new Uri($"{EmployeesPath}/{id}", UriKind.Relative), CancellationToken);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.InternalServerError, 9001, Unexpected, $"{EmployeesPath}/{id}", CancellationToken);
        problem.GetRawText().Should().NotContain("secret_column").And.NotContain("InvalidOperationException").And.NotContain(" at ");

        var events = factory.Logs.Events;
        var unhandled = events.Where(e => e.EventId() == 1).Should().ContainSingle("전역 예외 처리기 이벤트 1은 한 번만").Which;
        unhandled.Level.Should().Be(LogEventLevel.Error);
        unhandled.SourceContext().Should().Be("EmergencyHub.BuildingBlocks.Api.Exceptions.GlobalExceptionHandler");
        unhandled.Properties["ExceptionType"].ToString().Should().Be("\"System.InvalidOperationException\"");
        unhandled.Exception!.StackTrace.Should().Contain(nameof(ThrowingEmployeeReadRepository), "원인 추적은 형식 이름 · 스택으로 한다");

        var completion = factory.Logs.RequestCompletions.Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Error);
        completion.Properties["StatusCode"].ToString().Should().Be("500");

        events.Where(e => e.Level >= LogEventLevel.Error).Should().HaveCount(2, "Error는 이벤트 1과 요청 완료 로그 두 줄뿐이다");
        events.Should().NotContain(e => e.SourceContext().Contains("ExceptionHandlerMiddleware", StringComparison.Ordinal));
        events.Select(LogEventText.Dump).Should().NotContain(text => text.Contains("secret_column", StringComparison.Ordinal), "원본 예외 메시지는 로그 어디에도 없다");
    }

    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    /// <summary>조회에서 예상하지 못한 예외를 던지는 Read Repository 대역입니다(테스트 전용 Controller 대신 DI 교체).</summary>
    private sealed class ThrowingEmployeeReadRepository : IEmployeeReadRepository
    {
        public Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SecretMessage);
    }
}
