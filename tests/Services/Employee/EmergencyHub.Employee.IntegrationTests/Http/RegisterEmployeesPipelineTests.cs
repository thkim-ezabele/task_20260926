using System.Net;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using EmergencyHub.ServiceDefaults;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S06-T06 완료 조건 ⑦(S05 증빙 대응표 HTTP "이전 대기: S06-T06" 행): 새 일괄 등록 엔드포인트로 옮긴 HTTP 파이프라인 확인.
// - 500 형식(이전 ProblemDetailsHttpTests.Get_UnhandledExceptionInReadRepository_…): 처리되지 않은 예외 → 9001, 원본 메시지 없음, 이벤트 1 한 번(Error)
// - 읽기 전용 연결로 쓰기(이전 ReadOnlyWriteRejectionHttpTests.*): Production에서 25006 → 500 · 9001, SQL · 제약 이름 · SQLSTATE 미노출, 0건
// - 요청 완료 로그(이전 RequestCompletionLogTests.*): 비헬스 요청 1건당 1줄(4xx Information, 5xx Error), 헬스 요청 0줄
// - OpenAPI 계약(이전 OpenApiContractHttpTests.*): 실제 호스트 swagger.json의 POST /api/employee 응답 키 · 요청 본문 형식
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-05")]
[Trait("FR", "PRD-002/FR-09")]
[Trait("FR", "PRD-002/FR-10")]
public sealed class RegisterEmployeesPipelineTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string GlobalExceptionHandler = "EmergencyHub.BuildingBlocks.Api.Exceptions.GlobalExceptionHandler";

    private const string SecretMessage = "SELECT secret_column FROM employees WHERE normalized_email = 'leak@example.com' -- ux_employees_normalized_email";

    // 응답 본문 · 수집 로그 어디에도 나오면 안 되는 문자열: SQL, 테이블 · 제약 · 인덱스 이름, SQLSTATE, 서버 원본 메시지, 원본 예외 로그 문구.
    private static readonly string[] ForbiddenFragments =
    [
        "INSERT INTO",
        "RETURNING",
        "ux_employees_normalized_email",
        "pk_employees",
        "25006",
        "read-only transaction",
        "An unhandled exception has occurred while executing the request",
    ];

    // ---- 성공: 요청 완료 로그 1줄 · OpenAPI 계약 ----

    [Fact]
    public async Task Post_Created_WritesExactlyOneCompletionLogAtInformation()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(1), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var completion = factory.Logs.RequestCompletions.Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Information);
        completion.Properties["RequestMethod"].ToString().Should().Be("\"POST\"");
        completion.Properties["RequestPath"].ToString().Should().Be($"\"{ImportContent.RegisterPath}\"");
        completion.Properties["StatusCode"].ToString().Should().Be("201");
        completion.Properties["ServiceName"].ToString().Should().Be("\"employee\"");
    }

    [Fact]
    public async Task SwaggerJson_Development_DeclaresRegisterResponsesAndFourRequestBodyTypes()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        var post = document.RootElement.GetProperty("paths").GetProperty(ImportContent.RegisterPath).GetProperty("post");
        var responses = post.GetProperty("responses");
        responses.EnumerateObject().Select(item => item.Name).Should().Equal("201", "400", "409", "413", "415");
        foreach (var failure in new[] { "400", "409", "413", "415" })
        {
            responses.GetProperty(failure).GetProperty("content").EnumerateObject().Select(item => item.Name)
                .Should().Equal([HttpProblem.ContentType], "실패 응답은 problem+json");
        }

        var requestBody = post.GetProperty("requestBody");
        requestBody.GetProperty("content").EnumerateObject().Select(item => item.Name)
            .Should().Equal("multipart/form-data", "application/x-www-form-urlencoded", "text/csv", "application/json");
        var multipart = requestBody.GetProperty("content").GetProperty("multipart/form-data").GetProperty("schema").GetProperty("properties");
        multipart.GetProperty("file").GetProperty("format").GetString().Should().Be("binary", "Swagger UI에서 파일을 고를 수 있다");
        multipart.GetProperty("data").GetProperty("type").GetString().Should().Be("string");
        post.TryGetProperty("parameters", out _).Should().BeFalse("바인더 매개변수는 요청 본문으로만 나온다");
    }

    // ---- 실패: 500 형식 · 읽기 전용 연결 ----

    [Fact]
    public async Task Post_UnhandledExceptionInHandlerPath_Returns500With9001WithoutOriginalMessageAndLogsOnceAtError()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            AfterEmailLookup = (_, _) => throw new InvalidOperationException(SecretMessage),
        });
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(1), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.InternalServerError, 9001, CommonErrors.Unexpected.Message, ImportContent.RegisterPath, CancellationToken);
        problem.GetRawText().Should().NotContain("secret_column").And.NotContain("InvalidOperationException").And.NotContain(" at ");

        var events = factory.Logs.Events;
        var unhandled = events.Should().ContainSingle(logEvent => IsGlobalExceptionEvent(logEvent), "전역 예외 처리기 이벤트 1은 한 번만").Which;
        unhandled.Level.Should().Be(LogEventLevel.Error);
        unhandled.SourceContext().Should().Be(GlobalExceptionHandler);
        unhandled.Properties["ExceptionType"].ToString().Should().Be("\"System.InvalidOperationException\"");
        unhandled.Exception!.StackTrace.Should().Contain("EmailLookupHookRepository", "원인 추적은 형식 이름 · 스택으로 한다");
        var completion = factory.Logs.RequestCompletions.Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Error);
        completion.Properties["StatusCode"].ToString().Should().Be("500");
        events.UnexpectedErrors(1).Should().Equal([completion], "Error는 이벤트 1과 요청 완료 로그 두 줄뿐이다");
        events.Should().NotContain(logEvent => logEvent.SourceContext().Contains("ExceptionHandlerMiddleware", StringComparison.Ordinal));
        events.Select(LogEventText.Dump).Should().NotContain(text => text.Contains("secret_column", StringComparison.Ordinal), "원본 예외 메시지는 로그 어디에도 없다");
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Post_WriteDbContextOnReadOnlyConnectionInProduction_Returns500With9001AndLeaksNoSqlOrConstraint()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            Environment = "Production",
            WriteConnectionString = Database.ReadConnectionString,
        });
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(2, "ro"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.InternalServerError, 9001, CommonErrors.Unexpected.Message, ImportContent.RegisterPath, CancellationToken);
        problem.GetRawText().Should().NotContainAny(ForbiddenFragments).And.NotContain("Npgsql").And.NotContain("DbUpdateException");

        var events = factory.Logs.Events;
        var unhandled = events.Should().ContainSingle(logEvent => IsGlobalExceptionEvent(logEvent)).Which;
        unhandled.Properties["ExceptionType"].ToString().Should().Be("\"Microsoft.EntityFrameworkCore.DbUpdateException\"");
        unhandled.Exception!.InnerException.Should().NotBeNull("형식 이름 · 스택은 내부 예외 사슬까지 남는다(메시지만 뺌)");
        events.UnexpectedErrors(1).Should().ContainSingle().Which.MessageTemplate.Text.Should().Be(SerilogEventCollector.RequestCompletionTemplate);
        events.Select(LogEventText.Dump).Should().AllSatisfy(text => text.Should().NotContainAny(ForbiddenFragments));
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0, "25006으로 거부되어 저장된 행이 없다");
    }

    [Fact]
    public async Task Post_ValidationFailure_WritesOneCompletionLogWith400AtInformation()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var content = ImportContent.FormUrlEncoded("note=x");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var completion = factory.Logs.RequestCompletions.Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Information, "4xx는 Error가 아니다(RequestLogLevels)");
        completion.Properties["StatusCode"].ToString().Should().Be("400");
    }

    // ---- 엣지: 헬스 요청은 완료 로그를 남기지 않는다 ----

    [Theory]
    [InlineData(HealthEndpoints.LivePath)]
    [InlineData(HealthEndpoints.ReadyPath)]
    public async Task GetHealth_WritesNoCompletionLog(string path)
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        factory.Logs.Clear();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.Logs.RequestCompletions.Should().BeEmpty("헬스 요청은 Verbose로 낮춰 Development 최소 수준(Debug)에서 걸러진다");
    }

    // 이벤트 ID 1은 전역 예외 처리기 전용이지만 프레임워크 범주도 1을 쓴다(예: TestServer의 RequestSizeLimitFilter 경고). 범주로 좁힌다.
    private static bool IsGlobalExceptionEvent(LogEvent logEvent) => logEvent.EventId() == 1 && logEvent.SourceContext() == GlobalExceptionHandler;
}
