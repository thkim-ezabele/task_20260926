using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Api.UnitTests.TestDoubles;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace EmergencyHub.Employee.Api.UnitTests.Acceptance;

// S07-T02(PRD-002 FR-08, ADR-0025): 운영 등록으로 띄운 TestServer에서 GET /api/employee/{name}의 라우트 값 디코딩 · 응답 JSON · ProblemDetails를 확인하고,
// 응답 본문 · instance · 요청 완료 로그 · 요청 안의 모든 로그에 이름 값이 없는지 본다(NFR-04). ISender만 대역이라 Validator · Handler · DB는 실행하지 않는다
// (전 구간 200 · 400 · 404 · 500과 추적 span은 S07-T03 통합 테스트).
[Trait("FR", "PRD-002/FR-08")]
[Trait("NFR", "PRD-002/NFR-04")]
public sealed class GetEmployeeByNameHttpTests : IAsyncLifetime
{
    private const string Name = "홍길동";
    private const string EncodedName = "%ED%99%8D%EA%B8%B8%EB%8F%99";
    private const string Template = "/api/employee/{name}";
    private const string CompletionTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    private static readonly Guid EmployeeId = Guid.Parse("0192a1b3-0000-7000-8000-000000000001");

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly List<GetEmployeeByNameQuery> _queries = [];
    private readonly CollectingLogSink _sink = new();
    private EmployeeApiTestHost _host = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _host = await EmployeeApiTestHost.StartAsync(_sender, "Production", CancellationToken, builder =>
        {
            // 서비스 appsettings.json과 같은 Microsoft.AspNetCore Warning 재정의(호스팅 "Request starting <URL>" 로그를 막는 전제, S07-T03에서 고정).
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning",
            });
            builder.Services.AddSingleton<ILogEventSink>(_sink);
        });
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    // ---- 성공 ----

    [Fact]
    public async Task Get_EncodedKoreanName_Returns200WithContactFieldsAndSendsDecodedName()
    {
        Returns(Result.Success(new EmployeeResponse(EmployeeId, Name, "hong@example.com", "010-1234-5678", new DateOnly(2020, 1, 2))));

        using var response = await _host.Client.GetAsync($"/api/employee/{EncodedName}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal("id", "name", "email", "tel", "joined");
        json.RootElement.GetProperty("name").GetString().Should().Be(Name);
        json.RootElement.GetProperty("joined").GetString().Should().Be("2020-01-02");
        _queries.Should().Equal(new GetEmployeeByNameQuery(Name));
        AssertCompletionAndLogsHaveNoName(200);
    }

    // ---- 실패 ----

    [Fact]
    public async Task Get_NotFound_Returns404With22001AndTemplateInstance()
    {
        Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));

        using var response = await _host.Client.GetAsync($"/api/employee/{EncodedName}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("code").GetInt32().Should().Be(22001);
        json.RootElement.GetProperty("instance").GetString().Should().Be(Template);
        AssertNoName(body);
        AssertCompletionAndLogsHaveNoName(404);
    }

    [Fact]
    public async Task Get_BlankName_SendsNullNameAndReturns400With21007()
    {
        // 끝 슬래시(/api/employee/)는 목록 라우트라 공백 이름은 %20으로 보낸다. 실측: 단순 형식 바인더가 공백만 있는 값을 null로 바꾼다(ConvertEmptyStringToNull).
        Returns(Result.Failure<EmployeeResponse>(ValidationError.Create([FieldError.Create(nameof(GetEmployeeByNameQuery.Name), EmployeeErrors.NameRequired)])));

        using var response = await _host.Client.GetAsync("/api/employee/%20", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("code").GetInt32().Should().Be(CommonErrors.ValidationFailed.Code);
        json.RootElement.GetProperty("errors").GetProperty("name")[0].GetProperty("code").GetInt32().Should().Be(21007);
        json.RootElement.GetProperty("instance").GetString().Should().Be(Template);
        _queries.Should().Equal(new GetEmployeeByNameQuery(null));
    }

    [Fact]
    public async Task Get_UnexpectedException_Returns500With9001AndTemplateInstanceAndNoNameInLogs()
    {
        _sender.QueryAsync(Arg.Any<GetEmployeeByNameQuery>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<EmployeeResponse>>>(_ => throw new InvalidOperationException("boom"));

        using var response = await _host.Client.GetAsync($"/api/employee/{EncodedName}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("code").GetInt32().Should().Be(CommonErrors.Unexpected.Code);
        json.RootElement.GetProperty("instance").GetString().Should().Be(Template);
        AssertNoName(body);
        var handlerLog = _sink.Events.Should().ContainSingle(logEvent => logEvent.Level == LogEventLevel.Error && logEvent.MessageTemplate.Text != CompletionTemplate,
            "전역 예외 처리기의 Error 로그(요청 안 ILogger 로그)").Subject;
        handlerLog.Properties["RequestPath"].ToString().Should().Be($"\"{Template}\"", "호스팅 로그 범위의 RequestPath도 템플릿으로 덮는다");
        AssertCompletionAndLogsHaveNoName(500);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Get_NfdEncodedName_SendsNfdAsIsForHandlerToNormalize()
    {
        // NFC 정규화는 Name.Create(Validator · Handler) 몫이라 Controller는 디코딩한 값을 그대로 넘긴다.
        var nfd = Name.Normalize(NormalizationForm.FormD);
        Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));

        using var response = await _host.Client.GetAsync($"/api/employee/{Uri.EscapeDataString(nfd)}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _queries.Should().Equal(new GetEmployeeByNameQuery(nfd));
    }

    [Fact]
    public async Task Get_CaseDifferentName_IsSentWithoutCaseFolding()
    {
        Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));

        using var response = await _host.Client.GetAsync("/api/employee/Hong", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _queries.Should().Equal(new GetEmployeeByNameQuery("Hong"));
    }

    private void Returns(Result<EmployeeResponse> result) =>
        _sender.QueryAsync(Arg.Do<GetEmployeeByNameQuery>(_queries.Add), Arg.Any<CancellationToken>()).Returns(result);

    private void AssertCompletionAndLogsHaveNoName(int status)
    {
        var completion = _sink.Events.Where(logEvent => logEvent.MessageTemplate.Text == CompletionTemplate).Should().ContainSingle().Subject;
        completion.Properties["RequestPath"].ToString().Should().Be($"\"{Template}\"");
        completion.Properties["StatusCode"].ToString().Should().Be(status.ToString(CultureInfo.InvariantCulture));
        foreach (var logEvent in _sink.Events)
        {
            AssertNoName(logEvent.RenderMessage(CultureInfo.InvariantCulture) + string.Join('|', logEvent.Properties.Select(pair => $"{pair.Key}={pair.Value}")));
        }
    }

    private static void AssertNoName(string text) => text.Should().NotContain(Name).And.NotContain(EncodedName);
}
