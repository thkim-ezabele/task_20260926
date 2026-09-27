using EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Events;

namespace EmergencyHub.ServiceDefaults.UnitTests;

// S03-T07 결함 수정(ADR-0020 "요청 로그", evidence/S03-T05 '발견 사항' 1): UseSerilogRequestLogging은 options.Logger가 없으면
// 정적 Serilog.Log에 쓰는데, AddServiceDefaults는 정적 로거를 바꾸지 않아(preserveStaticLogger: true) 요청 완료 로그가 사라졌다.
// AddServiceDefaults가 RequestLoggingOptions.Logger를 DI의 Serilog 로거로 채우는지 루프백 Kestrel 요청으로 확인한다.
[Trait("FR", "PRD-001/FR-03")]
public sealed class RequestLoggingTests
{
    private const string CompletionTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    private const string RequestLoggingSourceContext = "\"Serilog.AspNetCore.RequestLoggingMiddleware\"";

    // ---- 성공 ----

    [Fact]
    public async Task UseSerilogRequestLogging_AfterAddServiceDefaults_WritesOneCompletionEventToDiSink()
    {
        var sink = new CollectingSink();
        await using var app = await StartAsync(sink, _ => Results.Ok());
        using var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) };

        using var response = await client.GetAsync(new Uri("/ping", UriKind.Relative), TestContext.Current.CancellationToken);

        var completion = CompletionEvents(sink).Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Information);
        completion.Properties["RequestMethod"].ToString().Should().Be("\"GET\"");
        completion.Properties["RequestPath"].ToString().Should().Be("\"/ping\"");
        completion.Properties["StatusCode"].ToString().Should().Be("200");
        completion.Properties["SourceContext"].ToString().Should().Be(RequestLoggingSourceContext);
    }

    // ---- 실패 ----

    [Fact]
    public async Task UseSerilogRequestLogging_EndpointThrows_WritesCompletionEventAsErrorWithException()
    {
        var sink = new CollectingSink();
        await using var app = await StartAsync(sink, _ => throw new InvalidOperationException("boom"));
        using var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) };

        using var response = await client.GetAsync(new Uri("/ping", UriKind.Relative), TestContext.Current.CancellationToken);

        var completion = CompletionEvents(sink).Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Error);
        completion.Exception.Should().BeOfType<InvalidOperationException>();
        completion.Properties["StatusCode"].ToString().Should().Be("500");
    }

    // ---- 엣지 ----

    [Fact]
    public void AddServiceDefaults_WorkerBuilder_RequestLoggingOptionsLoggerIsDiSerilogLogger()
    {
        // MigrationService(Worker)도 같은 등록을 거친다. 옵션만 채우고 미들웨어는 없으니 동작 영향이 없다.
        var builder = TestHosts.CreateWorkerBuilder();
        builder.AddServiceDefaults();

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<RequestLoggingOptions>>().Value;
        options.Logger.Should().BeSameAs(host.Services.GetRequiredService<ILogger>());
        options.MessageTemplate.Should().Be(CompletionTemplate, "기본 템플릿은 바꾸지 않는다");
    }

    [Fact]
    public async Task UseSerilogRequestLogging_ExplicitLogger_IsNotOverridden()
    {
        // 호출 쪽이 options.Logger를 직접 주면 그 값이 이긴다(IOptions 값 위에 configureOptions가 적용됨).
        var diSink = new CollectingSink();
        var explicitSink = new CollectingSink();
        using var explicitLogger = new LoggerConfiguration().WriteTo.Sink(explicitSink).CreateLogger();
        await using var app = await StartAsync(diSink, _ => Results.Ok(), options => options.Logger = explicitLogger);
        using var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) };

        using var response = await client.GetAsync(new Uri("/ping", UriKind.Relative), TestContext.Current.CancellationToken);

        CompletionEvents(explicitSink).Should().ContainSingle();
        CompletionEvents(diSink).Should().BeEmpty();
    }

    private static IEnumerable<LogEvent> CompletionEvents(CollectingSink sink) =>
        sink.Events.Where(logEvent => logEvent.MessageTemplate.Text == CompletionTemplate);

    private static async Task<WebApplication> StartAsync(
        ILogEventSink sink,
        Func<HttpContext, IResult> endpoint,
        Action<RequestLoggingOptions>? configureOptions = null)
    {
        var builder = TestHosts.CreateWebBuilder("Testing");
        builder.Services.AddSingleton(sink);
        builder.AddServiceDefaults();

        var app = builder.Build();
        app.UseSerilogRequestLogging(configureOptions);
        app.MapGet("/ping", endpoint);

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
