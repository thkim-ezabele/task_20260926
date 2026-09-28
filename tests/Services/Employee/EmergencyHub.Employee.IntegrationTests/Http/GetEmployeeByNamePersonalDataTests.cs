using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.IntegrationTests.FaultInjection;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.Observability;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S07-T03 완료 조건 ④(PRD-002 NFR-04 · FR-10, ADR-0025 "이름 경로 매개변수와 개인정보"): {name} 라우트에 일치한 요청(200 · 400 · 404 · 500)의 응답 본문 · instance ·
// 요청 완료 로그 · 요청 안의 모든 로그 · ASP.NET Core 요청 span(url.path 포함) · Npgsql span에 이름 값이 없다. 200은 본문에 이름이 있는 것이 정상이라 본문만 판정에서 뺀다.
// 이름은 NFC · NFD 원문 × 대문자 · 소문자 퍼센트 인코딩 네 형태로 찾는다(PersonalValueForms.Name). 응답에 이메일 · 전화가 나오므로 판정 대상은 이름으로 한정한다.
// 405 · 라우트 불일치 404는 {name} 라우트에 일치하지 않아(ADR-0025 fallback) 판정 대상이 아니다. 환경은 Development(가장 넓은 로그 설정, EnableSensitiveDataLogging 끔).
// TestServer 요청은 traceparent의 trace ID로 서버 span을 고른다(HttpClient 계측 없음). 실제 Kestrel 경로는 200 한 건을 server.port로 고른다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-10")]
[Trait("NFR", "PRD-002/NFR-04")]
public sealed class GetEmployeeByNamePersonalDataTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string Name = "홍비밀";
    private const string Template = "/api/employee/{name}";

    // 리스너는 프로세스 전체에 걸리지만 컬렉션 테스트는 순차 실행이다. 보내기 직전에 비워 시드 · 호스트 시작 span을 뺀다.
    private readonly ActivityCollector _spans = new(ActivityCollector.AspNetCoreSource);
    private readonly ActivityCollector _npgsqlSpans = new(ActivityCollector.NpgsqlSource);

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        _spans.Dispose();
        _npgsqlSpans.Dispose();
        await base.DisposeAsync();
    }

    // ---- 성공: 200 ----

    [Fact]
    public async Task Get_200_LogsAndSpansContainNoName()
    {
        await using var factory = new EmployeeApiFactory(Database);
        await SeedAsync(factory, Name);

        var (body, traceId) = await SendAsync(factory, NamePath(Name), HttpStatusCode.OK);

        body.Should().Contain("\"id\"", "조회 결과(본문에 이름이 있는 것이 정상이라 본문은 판정하지 않음)");
        await ShouldNotExposeOutsideBodyAsync(factory, traceId, Name, 200);
        factory.Logs.Events.UnexpectedErrors(ExpectedLogEvent.GlobalException).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_200NfdEncodedOverKestrel_LogsAndServerSpanContainNoName()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { UseKestrel = true });
        using var client = factory.CreateKestrelClient();
        await SeedAsync(factory, Name);
        factory.Logs.Clear();
        using var spans = new ActivityCollector(ActivityCollector.AspNetCoreSource);

        using var response = await client.GetAsync(NamePath(Name.Normalize(NormalizationForm.FormD)), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var span = (await spans.WaitForAsync(ActivityCollector.ServerPort(factory.KestrelAddress.Port), CancellationToken)).Should().ContainSingle().Which;
        span.Tag("url.path").Should().Be(Template, "실제 Kestrel 요청에서도 url.path는 라우트 템플릿이다");
        PersonalDataScan.FindIn(span.Dump(), PersonalValueForms.Name(Name)).Should().BeEmpty();
        ShouldHaveTemplateCompletionAndNoNameInLogs(factory, Name, 200);
        factory.Logs.Events.UnexpectedErrors(ExpectedLogEvent.GlobalException).Should().BeEmpty();
    }

    // ---- 실패: 400(규칙 위반 이름) · 404 · 500(예외 메시지에 이름) ----

    [Fact]
    public async Task Get_400InvalidCharacterName_BodyLogsAndSpansContainNoName()
    {
        const string InvalidName = "홍\u0007비밀";
        await using var factory = new EmployeeApiFactory(Database);

        var (body, traceId) = await SendAsync(factory, NamePath(InvalidName), HttpStatusCode.BadRequest);

        body.Should().Contain(EmployeeErrors.NameInvalidCharacter.Code.ToString(CultureInfo.InvariantCulture));
        ShouldNotExposeInBody(body, InvalidName);
        await ShouldNotExposeOutsideBodyAsync(factory, traceId, InvalidName, 400);
        factory.Logs.Events.UnexpectedErrors(ExpectedLogEvent.GlobalException).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_404_BodyLogsAndSpansContainNoName()
    {
        await using var factory = new EmployeeApiFactory(Database);
        await SeedAsync(factory, "다른이름");

        var (body, traceId) = await SendAsync(factory, NamePath(Name), HttpStatusCode.NotFound);

        body.Should().Contain(EmployeeErrors.NotFound.Code.ToString(CultureInfo.InvariantCulture));
        ShouldNotExposeInBody(body, Name);
        await ShouldNotExposeOutsideBodyAsync(factory, traceId, Name, 404);
        factory.Logs.Events.UnexpectedErrors(ExpectedLogEvent.GlobalException).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_500ExceptionMessageCarryingName_BodyLogsAndSpansContainNoName()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            ConfigureServices = services => services.Decorate<IEmployeeReadRepository>((inner, _) => new ThrowingNameLookupReadRepository(inner)),
        });

        var (body, traceId) = await SendAsync(factory, NamePath(Name), HttpStatusCode.InternalServerError);

        body.Should().Contain(CommonErrors.Unexpected.Code.ToString(CultureInfo.InvariantCulture));
        ShouldNotExposeInBody(body, Name);
        await ShouldNotExposeOutsideBodyAsync(factory, traceId, Name, 500);
        factory.Logs.Events.Where(ExpectedLogEvent.GlobalException.Matches).Should().ContainSingle("전역 예외 처리기 경로를 지난다(테스트가 일부러 낸 예외)")
            .Which.Properties["RequestPath"].ToString().Should().Be($"\"{Template}\"");
        factory.Logs.Events.UnexpectedErrors(ExpectedLogEvent.GlobalException).Should().ContainSingle("전역 예외 밖의 Error는 요청 완료 로그 한 줄뿐")
            .Which.MessageTemplate.Text.Should().Be(SerilogEventCollector.RequestCompletionTemplate);
    }

    private static Uri NamePath(string name) => new("/api/employee/" + Uri.EscapeDataString(name), UriKind.Relative);

    private static async Task SeedAsync(EmployeeApiFactory factory, string name) =>
        (await EmployeeCommits.AddAndCommitAsync(factory.Services, new EmployeeBuilder().WithName(name).Build(), CancellationToken)).IsSuccess.Should().BeTrue();

    private static void ShouldNotExposeInBody(string body, string name)
    {
        PersonalDataScan.FindInJson(body, PersonalValueForms.Name(name)).Should().BeEmpty("응답 본문(detail · instance · errors)에 이름이 없다");
        body.Should().Contain($"\"instance\":\"{Template}\"", "instance는 라우트 템플릿");
    }

    private static void ShouldHaveTemplateCompletionAndNoNameInLogs(EmployeeApiFactory factory, string name, int status)
    {
        var completion = factory.Logs.RequestCompletions.Should().ContainSingle().Which;
        completion.Properties["RequestPath"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be(Template);
        completion.Properties["StatusCode"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be(status);
        PersonalDataScan.FindInLogs(factory.Logs.Events, PersonalValueForms.Name(name)).Should().BeEmpty("요청 안의 모든 로그에 이름이 없다");
    }

    // 로그 · 요청 span · Npgsql span을 모으며 보낸다(호스트 시작 로그는 뺌).
    private async Task<(string Body, string TraceId)> SendAsync(EmployeeApiFactory factory, Uri path, HttpStatusCode status)
    {
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        _spans.Clear();
        _npgsqlSpans.Clear();
        var traceId = ActivityTraceId.CreateRandom().ToHexString();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom().ToHexString()}-01");

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(status);
        return (await response.Content.ReadAsStringAsync(CancellationToken), traceId);
    }

    private async Task ShouldNotExposeOutsideBodyAsync(EmployeeApiFactory factory, string traceId, string name, int status)
    {
        var forms = PersonalValueForms.Name(name);
        ShouldHaveTemplateCompletionAndNoNameInLogs(factory, name, status);

        var span = (await _spans.WaitForAsync(activity => activity.TraceId == traceId, CancellationToken)).Should().ContainSingle().Which;
        span.Tag("url.path").Should().Be(Template, "url.path는 요청 경로 대신 라우트 템플릿");
        span.Tag("http.response.status_code").Should().Be(status.ToString(CultureInfo.InvariantCulture));
        PersonalDataScan.FindIn(span.Dump(), forms).Should().BeEmpty("요청 span 태그 · 이벤트 · 상태 설명에 이름이 없다");

        foreach (var npgsqlSpan in _npgsqlSpans.Activities)
        {
            PersonalDataScan.FindIn(npgsqlSpan.Dump(), forms).Should().BeEmpty("Npgsql span(db.statement)에 SQL 매개변수 값(이름)이 없다");
        }
    }
}
