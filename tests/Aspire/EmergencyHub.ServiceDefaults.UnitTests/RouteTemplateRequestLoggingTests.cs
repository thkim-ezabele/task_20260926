using EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace EmergencyHub.ServiceDefaults.UnitTests;

// S07-T02(ADR-0025 "이름 경로 매개변수와 개인정보"): 요청 완료 로그의 RequestPath는 라우트 템플릿이 있으면 템플릿, 없으면 요청 경로다.
// AddServiceDefaults가 RequestLoggingOptions.GetMessageTemplateProperties를 바꾸므로 UseSerilogRequestLogging을 쓰는 모든 서비스에 적용된다.
// 루프백 Kestrel에 실제 요청을 보내고 DI 수집 싱크로 받는다(RequestLoggingTests와 같은 방식). PathBase는 붙이지 않는다(instance만).
[Trait("FR", "PRD-002/FR-08")]
[Trait("NFR", "PRD-002/NFR-04")]
public sealed class RouteTemplateRequestLoggingTests
{
    private const string CompletionTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    private const string Name = "홍길동";
    private const string EncodedName = "%ED%99%8D%EA%B8%B8%EB%8F%99";

    // ---- 성공: 템플릿 있음 ----

    [Fact]
    public async Task Completion_RouteWithName_RequestPathIsTemplate()
    {
        var sink = new CollectingSink();
        await using var app = await StartAsync(sink, map: app => app.MapGet("/items/{name}", (string name) => Results.Ok()));

        await GetAsync(app, $"/items/{EncodedName}?q=secret");

        var completion = SingleCompletion(sink);
        completion.Properties["RequestPath"].ToString().Should().Be("\"/items/{name}\"");
        completion.Properties["StatusCode"].ToString().Should().Be("200");
        completion.Properties.Keys.Should().Contain(["RequestMethod", "RequestPath", "StatusCode", "Elapsed"]);
        AssertNoNameInAnyEvent(sink);
    }

    [Fact]
    public async Task Completion_TemplateCase_IsKept()
    {
        var sink = new CollectingSink();
        await using var app = await StartAsync(sink, map: app => app.MapGet("/Items/{Name}", (string name) => Results.Ok()));

        await GetAsync(app, $"/items/{EncodedName}");

        SingleCompletion(sink).Properties["RequestPath"].ToString().Should().Be("\"/Items/{Name}\"");
    }

    // ---- 실패: 템플릿 없음 → 요청 경로 유지 ----

    [Fact]
    public async Task Completion_NoMatchingEndpoint_RequestPathKeepsRequestPath()
    {
        var sink = new CollectingSink();
        await using var app = await StartAsync(sink, map: app => app.MapGet("/items/{name}", (string name) => Results.Ok()));

        await GetAsync(app, "/missing/path?q=secret");

        var completion = SingleCompletion(sink);
        completion.Properties["RequestPath"].ToString().Should().Be("\"/missing/path\"", "쿼리 문자열은 지금처럼 넣지 않는다");
        completion.Properties["StatusCode"].ToString().Should().Be("404");
    }

    [Fact]
    public async Task Completion_MethodNotAllowed_RequestPathKeepsRequestPath()
    {
        // 실측: 405는 라우팅이 RouteEndpoint가 아닌 엔드포인트("405 HTTP Method Not Supported")를 골라 템플릿이 없다(ADR-0025 fallback).
        var sink = new CollectingSink();
        await using var app = await StartAsync(sink, map: app => app.MapGet("/items/{name}", (string name) => Results.Ok()));

        using (var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) })
        using (var content = new StringContent(string.Empty))
        using (var response = await client.PostAsync(new Uri("/items/abc", UriKind.Relative), content, TestContext.Current.CancellationToken))
        {
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.MethodNotAllowed);
        }

        SingleCompletion(sink).Properties["RequestPath"].ToString().Should().Be("\"/items/abc\"");
    }

    // ---- 500 경로 ----

    [Fact]
    public async Task Completion_ExceptionHandledByExceptionHandler_RequestPathIsTemplateFromExceptionHandlerFeature()
    {
        // 서비스 파이프라인과 같은 순서(요청 로그 → 예외 처리): 예외 처리기가 엔드포인트를 지운 뒤에도 템플릿이다.
        var sink = new CollectingSink();
        await using var app = await StartAsync(
            sink,
            configure: app => app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return Task.CompletedTask;
                },
            }),
            map: app => app.MapGet("/items/{name}", IResult (string name) => throw new InvalidOperationException("boom")));

        await GetAsync(app, $"/items/{EncodedName}");

        var completion = SingleCompletion(sink);
        completion.Level.Should().Be(LogEventLevel.Error);
        completion.Properties["StatusCode"].ToString().Should().Be("500");
        completion.Properties["RequestPath"].ToString().Should().Be("\"/items/{name}\"");
        AssertNoNameInAnyEvent(sink);
    }

    [Fact]
    public async Task Completion_ExceptionReachingRequestLogging_RequestPathIsTemplate()
    {
        // 예외 처리기가 없어 예외가 요청 로그 미들웨어까지 올라오는 경우(엔드포인트가 그대로 남음).
        var sink = new CollectingSink();
        await using var app = await StartAsync(sink, map: app => app.MapGet("/items/{name}", IResult (string name) => throw new InvalidOperationException("boom")));

        await GetAsync(app, $"/items/{EncodedName}");

        var completion = SingleCompletion(sink);
        completion.Exception.Should().BeOfType<InvalidOperationException>();
        completion.Properties["RequestPath"].ToString().Should().Be("\"/items/{name}\"");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Completion_PathBase_IsNotPrependedToTemplate()
    {
        var sink = new CollectingSink();
        await using var app = await StartAsync(
            sink,
            configure: app => app.UsePathBase("/svc"),
            map: app => app.MapGet("/items/{name}", (string name) => Results.Ok()));

        await GetAsync(app, $"/svc/items/{EncodedName}");

        SingleCompletion(sink).Properties["RequestPath"].ToString().Should().Be("\"/items/{name}\"", "PathBase는 instance만 붙인다");
    }

    [Fact]
    public async Task ApplicationLogDuringNameRequest_HasNoNameInAnyProperty()
    {
        // 요청 안에서 ILogger<T>로 남긴 로그에도 요청 경로(호스팅 로그 범위 RequestPath)가 이름 값으로 남지 않는다(NFR-04).
        var sink = new CollectingSink();
        await using var app = await StartAsync(sink, map: app => app.MapGet("/items/{name}", (string name, ILogger<RouteTemplateRequestLoggingTests> logger) =>
        {
            logger.WarningSample();
            return Results.Ok();
        }));

        await GetAsync(app, $"/items/{EncodedName}");

        var applicationLog = sink.Events.Should().ContainSingle(logEvent => logEvent.MessageTemplate.Text == "Warning sample").Subject;
        applicationLog.Properties.Should().ContainKey("RequestPath");
        applicationLog.Properties["RequestPath"].ToString().Should().Be("\"/items/{name}\"");
        AssertNoNameInAnyEvent(sink);
    }

    private static LogEvent SingleCompletion(CollectingSink sink) =>
        sink.Events.Where(logEvent => logEvent.MessageTemplate.Text == CompletionTemplate).Should().ContainSingle().Subject;

    private static void AssertNoNameInAnyEvent(CollectingSink sink)
    {
        sink.Events.Should().NotBeEmpty();
        foreach (var logEvent in sink.Events)
        {
            var text = logEvent.RenderMessage(System.Globalization.CultureInfo.InvariantCulture) + string.Join('|', logEvent.Properties.Select(pair => $"{pair.Key}={pair.Value}"));
            text.Should().NotContain(Name).And.NotContain(EncodedName, $"'{logEvent.MessageTemplate.Text}' 로그에 이름 값이 없어야 한다");
        }
    }

    private static async Task GetAsync(WebApplication app, string pathAndQuery)
    {
        using var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) };
        using var response = await client.GetAsync(new Uri(pathAndQuery, UriKind.Relative), TestContext.Current.CancellationToken);
    }

    private static async Task<WebApplication> StartAsync(
        CollectingSink sink,
        Action<WebApplication>? configure = null,
        Action<WebApplication>? map = null)
    {
        var builder = TestHosts.CreateWebBuilder("Testing");

        // 서비스 appsettings.json과 같이 Microsoft.AspNetCore는 Warning이다. 호스팅의 "Request starting <URL>" Information 로그는
        // 요청 경로를 메시지에 담으므로 이 재정의가 막는다(전제, S07-T03에서 서비스 설정으로 고정).
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning",
        });
        builder.Services.AddSingleton<Serilog.Core.ILogEventSink>(sink);
        builder.AddServiceDefaults();

        var app = builder.Build();
        app.UseSerilogRequestLogging();
        configure?.Invoke(app);
        app.UseRouting();
        map?.Invoke(app);

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
