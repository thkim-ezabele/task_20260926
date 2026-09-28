using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Api.UnitTests.TestDoubles;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Api.UnitTests.Acceptance;

// S06-T05 완료 조건 ② · ③(PRD-002 FR-05 입력 경로 표, FR-09 ①, NFR-01): 운영 등록으로 띄운 TestServer에서 라우팅 · 필터 · 전용 바인더 · 예외 처리를 지난다.
// ISender만 대역이라 Validator · Handler · DB는 실행하지 않는다(입력 경로 표 전체 · 0건 저장 · Kestrel 413은 S06-T06 통합 테스트).
// 한도 초과 도달 위치(실측): 폼 값 공급자를 뺀 이 액션에서는 네 경로 모두 바인더가 본문 바이트 수로 판정해 BadHttpRequestException(413) → 전역 처리기 1004.
[Trait("FR", "PRD-002/FR-05")]
[Trait("FR", "PRD-002/FR-09")]
[Trait("NFR", "PRD-002/NFR-01")]
public sealed class RegisterEmployeesHttpTests : IAsyncLifetime
{
    private const string Path = "/api/employee";
    private const string ProblemJson = "application/problem+json";
    private const int OneMebibyte = 1_048_576;

    private static readonly Guid RegisteredId = Guid.Parse("0192a1b3-0000-7000-8000-000000000001");
    private static readonly string CsvRow = "김이름,kim@gmail.com,010-0000-0000,2000-01-01";
    private static readonly string JsonList = """{"name":"김이름","email":"kim@gmail.com","tel":"010-0000-0000","joined":"2000-01-01"},{"name":"이이름","email":"lee@gmail.com","tel":"010-0000-0001","joined":"2000-01-02"}""";

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly List<RegisterEmployeesCommand> _commands = [];
    private EmployeeApiTestHost _host = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _sender.SendAsync(Arg.Do<RegisterEmployeesCommand>(_commands.Add), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new RegisterEmployeesResponse(1, [RegisteredId])));
        _host = await EmployeeApiTestHost.StartAsync(_sender, "Production", CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    // ---- 성공: 입력 경로 네 가지 → 201, Location 없음 ----

    public static TheoryData<string, EmployeeImportSources, EmployeeImportFormat> SupportedPaths() => new()
    {
        { "multipart-file-csv", EmployeeImportSources.File, EmployeeImportFormat.Csv },
        { "multipart-file-json", EmployeeImportSources.File, EmployeeImportFormat.Json },
        { "multipart-data-csv", EmployeeImportSources.Data, EmployeeImportFormat.Csv },
        { "multipart-data-json", EmployeeImportSources.Data, EmployeeImportFormat.Json },
        { "form-data-csv", EmployeeImportSources.Data, EmployeeImportFormat.Csv },
        { "form-data-json", EmployeeImportSources.Data, EmployeeImportFormat.Json },
        { "raw-csv", EmployeeImportSources.Body, EmployeeImportFormat.Csv },
        { "raw-json", EmployeeImportSources.Body, EmployeeImportFormat.Json },
    };

    [Theory]
    [MemberData(nameof(SupportedPaths))]
    public async Task Post_SupportedPath_Returns201WithCountAndIdsWithoutLocation(string path, EmployeeImportSources sources, EmployeeImportFormat format)
    {
        var text = format == EmployeeImportFormat.Json ? JsonList : CsvRow;
        using var content = CreateContent(path, Encoding.UTF8.GetBytes(text));

        using var response = await PostAsync(content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().BeNull("ADR-0025: 일괄 201은 Location을 생략한다");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("count").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("ids").EnumerateArray().Select(id => id.GetGuid()).Should().Equal(RegisteredId);

        var command = _commands.Should().ContainSingle().Subject;
        command.Sources.Should().Be(sources);
        command.Format.Should().Be(format);
        Encoding.UTF8.GetString(command.Content.Span).Should().Be(text);
    }

    // tester 보강(S06-T05 ②): [Consumes] 일치는 매개변수 · 대소문자를 무시해 raw 두 형식이 액션까지 온다.
    [Theory]
    [InlineData("application/json; charset=utf-8", EmployeeImportFormat.Json)]
    [InlineData("text/csv; charset=utf-8", EmployeeImportFormat.Csv)]
    [InlineData("TEXT/CSV", EmployeeImportFormat.Csv)]
    public async Task Post_RawContentTypeWithParameterOrUpperCase_Returns201(string contentType, EmployeeImportFormat format)
    {
        var text = format == EmployeeImportFormat.Json ? JsonList : CsvRow;
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        using var response = await PostAsync(content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var command = _commands.Should().ContainSingle().Subject;
        command.Sources.Should().Be(EmployeeImportSources.Body);
        command.Format.Should().Be(format);
    }

    [Fact]
    public async Task Post_NoContentType_ReachesBinderAndDetectsFromContent()
    {
        // T01 결정의 판정 결과: Content-Type 없는 요청은 [Consumes]를 지나 바인더가 raw body로 내용 판별한다(415 아님).
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonList));

        using var response = await PostAsync(content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var command = _commands.Should().ContainSingle().Subject;
        command.Sources.Should().Be(EmployeeImportSources.Body);
        command.Format.Should().Be(EmployeeImportFormat.Json);
    }

    // ---- 실패: 415 · 413 · 400(전송 형식) ----

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("application/octet-stream")]
    public async Task Post_UnsupportedContentType_Returns415With1005(string contentType)
    {
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(CsvRow));
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await PostAsync(content);

        await ShouldBeProblemAsync(response, HttpStatusCode.UnsupportedMediaType, 1005);
        _commands.Should().BeEmpty();
    }

    // tester 보강(S06-T05 ②): 접미사 +json 형식은 [Consumes("application/json")]의 부분 집합으로 통과한다.
    // 바인더가 지원하지 않는 형식으로 거절한 경로(ModelState UnsupportedContentTypeException)도 같은 415 · 1005여야 한다.
    [Theory]
    [InlineData("application/problem+json")]
    [InlineData("application/merge-patch+json")]
    public async Task Post_StructuredJsonSuffixContentType_PassesConsumesButBinderReturns415With1005(string contentType)
    {
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonList));
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await PostAsync(content);

        await ShouldBeProblemAsync(response, HttpStatusCode.UnsupportedMediaType, 1005);
        _commands.Should().BeEmpty();
    }

    [Theory]
    [InlineData("multipart-file-csv")]
    [InlineData("multipart-data-csv")]
    [InlineData("form-data-csv")]
    [InlineData("raw-csv")]
    public async Task Post_BodyOverOneMebibyte_Returns413With1004WithoutSending(string path)
    {
        using var content = CreateContent(path, Enumerable.Repeat((byte)'a', OneMebibyte + 1).ToArray());

        using var response = await PostAsync(content);

        await ShouldBeProblemAsync(response, HttpStatusCode.RequestEntityTooLarge, 1004);
        _commands.Should().BeEmpty();
    }

    [Theory]
    [InlineData("multipart/form-data")]
    [InlineData("multipart/form-data; boundary=\"\"")]
    public async Task Post_MultipartWithoutBoundary_Returns400With1001(string contentType)
    {
        using var content = new ByteArrayContent(Encoding.ASCII.GetBytes("--x\r\nContent-Disposition: form-data; name=\"data\"\r\n\r\nabc\r\n--x--\r\n"));
        content.Headers.TryAddWithoutValidation("Content-Type", contentType);

        using var response = await PostAsync(content);

        var problem = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, 1001);
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).Should().Equal(string.Empty);
        _commands.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_TruncatedMultipart_Returns400With1001()
    {
        using var content = new ByteArrayContent(Encoding.ASCII.GetBytes("--x\r\nContent-Disposition: form-data; name=\"data\"\r\n\r\nabc"));
        content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=x");

        using var response = await PostAsync(content);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, 1001);
    }

    [Fact]
    public async Task Post_CommandFails_ReturnsProblemFromResult()
    {
        _sender.SendAsync(Arg.Any<RegisterEmployeesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RegisterEmployeesResponse>(ValidationError.Create([FieldError.Create(string.Empty, EmployeeErrors.ImportInputEmpty)])));
        using var content = new FormUrlEncodedContent([new("note", CsvRow)]);

        using var response = await PostAsync(content);

        var problem = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, 1001);
        problem.GetProperty("errors").GetProperty(string.Empty)[0].GetProperty("code").GetInt32().Should().Be(21028);
    }

    // ---- 엣지: 경계값, 메서드 불일치 ----

    [Fact]
    public async Task Post_RawBodyExactlyOneMebibyte_IsAccepted()
    {
        using var content = CreateContent("raw-csv", Enumerable.Repeat((byte)'a', OneMebibyte).ToArray());

        using var response = await PostAsync(content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _commands.Should().ContainSingle().Which.Content.Length.Should().Be(OneMebibyte);
    }

    [Fact]
    public async Task Post_FormUrlEncodedWithoutDataKey_SendsEmptySources()
    {
        // 입력 경로 표 "form-urlencoded, data 키 없음(curl -d '김이름,...')" → Validator가 21028(빈 입력)로 거부한다.
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(CsvRow));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        using var response = await PostAsync(content);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "ISender 대역은 검증하지 않는다");
        _commands.Should().ContainSingle().Which.Sources.Should().Be(EmployeeImportSources.None);
    }

    [Fact]
    public async Task Delete_RegisterRoute_Returns405WithoutBody()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(Path, UriKind.Relative));

        using var response = await _host.Client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().BeEmpty("415가 아닌 본문 없는 응답은 상태 코드 페이지가 바꾸지 않는다");
    }

    private static HttpContent CreateContent(string path, byte[] bytes)
    {
        switch (path)
        {
            case "multipart-file-csv" or "multipart-file-json":
                var file = new ByteArrayContent(bytes);
                file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return new MultipartFormDataContent { { file, "file", path.EndsWith("json", StringComparison.Ordinal) ? "employees.json" : "employees.csv" } };
            case "multipart-data-csv" or "multipart-data-json":
                return new MultipartFormDataContent { { new ByteArrayContent(bytes), "data" } };
            case "form-data-csv" or "form-data-json":
                var encoded = new ByteArrayContent(Encoding.ASCII.GetBytes("data=" + Uri.EscapeDataString(Encoding.UTF8.GetString(bytes))));
                encoded.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                return encoded;
            default:
                var raw = new ByteArrayContent(bytes);
                raw.Headers.ContentType = new MediaTypeHeaderValue(path.EndsWith("json", StringComparison.Ordinal) ? "application/json" : "text/csv");
                return raw;
        }
    }

    private static async Task<JsonElement> ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status, int code)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType?.MediaType.Should().Be(ProblemJson);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        var problem = json.RootElement.Clone();
        problem.GetProperty("code").GetInt32().Should().Be(code);
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        return problem;
    }

    private Task<HttpResponseMessage> PostAsync(HttpContent content) =>
        _host.Client.PostAsync(new Uri(Path, UriKind.Relative), content, CancellationToken);
}
