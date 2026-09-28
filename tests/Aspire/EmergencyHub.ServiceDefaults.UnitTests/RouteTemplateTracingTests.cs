using System.Collections.Concurrent;
using System.Diagnostics;
using EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace EmergencyHub.ServiceDefaults.UnitTests;

// S07-T02(ADR-0025 "이름 경로 매개변수와 개인정보"): ASP.NET Core 계측은 요청 시작 때 span의 url.path에 요청 경로를 넣는다.
// AddServiceDefaults가 EnrichWithHttpResponse(span 종료 때)에서 라우트 템플릿이 있으면 url.path를 템플릿으로 덮는다. 없으면 요청 경로 그대로다.
// ActivityListener로 "Microsoft.AspNetCore" span을 받고(새 패키지 없음), server.port로 이 테스트의 span만 고른다.
[Trait("FR", "PRD-002/FR-08")]
[Trait("NFR", "PRD-002/NFR-04")]
public sealed class RouteTemplateTracingTests : IDisposable
{
    private const string EncodedName = "%ED%99%8D%EA%B8%B8%EB%8F%99";

    private readonly ConcurrentQueue<Activity> _stopped = new();
    private readonly ActivityListener _listener;

    public RouteTemplateTracingTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stopped.Enqueue,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    // ---- 성공: 템플릿 있음 ----

    [Fact]
    public async Task Span_RouteWithName_UrlPathIsTemplate()
    {
        await using var app = await StartAsync(app => app.MapGet("/items/{name}", (string name) => Results.Ok()));

        var span = await SendAsync(app, $"/items/{EncodedName}?q=secret");

        span.GetTagItem("url.path").Should().Be("/items/{name}");
        span.GetTagItem("http.route").Should().Be("/items/{name}");
        span.GetTagItem("http.response.status_code").Should().Be(200);
        AssertNoName(span);
    }

    // ---- 실패: 템플릿 없음 → 요청 경로 유지 ----

    [Fact]
    public async Task Span_NoMatchingEndpoint_UrlPathKeepsRequestPath()
    {
        await using var app = await StartAsync(app => app.MapGet("/items/{name}", (string name) => Results.Ok()));

        var span = await SendAsync(app, "/missing/path");

        span.GetTagItem("url.path").Should().Be("/missing/path");
        span.GetTagItem("http.route").Should().BeNull();
        span.GetTagItem("http.response.status_code").Should().Be(404);
    }

    // ---- 500 경로 ----

    [Fact]
    public async Task Span_ExceptionHandledByExceptionHandler_UrlPathIsTemplate()
    {
        await using var app = await StartAsync(
            app => app.MapGet("/items/{name}", IResult (string name) => throw new InvalidOperationException("boom")),
            app => app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return Task.CompletedTask;
                },
            }));

        var span = await SendAsync(app, $"/items/{EncodedName}");

        span.GetTagItem("http.response.status_code").Should().Be(500);
        span.GetTagItem("url.path").Should().Be("/items/{name}");
        span.GetTagItem("http.route").Should().Be("/items/{name}", "실측(계측 1.19.0): 예외 처리기가 엔드포인트를 지워도 http.route는 남는다");
        AssertNoName(span);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Span_PathBase_IsNotPrependedToTemplate()
    {
        await using var app = await StartAsync(app => app.MapGet("/items/{name}", (string name) => Results.Ok()), app => app.UsePathBase("/svc"));

        var span = await SendAsync(app, $"/svc/items/{EncodedName}");

        span.GetTagItem("url.path").Should().Be("/items/{name}");
    }

    private static void AssertNoName(Activity span)
    {
        foreach (var (key, value) in span.TagObjects)
        {
            (value?.ToString() ?? string.Empty).Should().NotContain(EncodedName).And.NotContain("홍길동", $"span 속성 {key}에 이름 값이 없어야 한다");
        }

        span.DisplayName.Should().NotContain(EncodedName);
    }

    private async Task<Activity> SendAsync(WebApplication app, string pathAndQuery)
    {
        var baseAddress = TestHosts.BaseAddressOf(app);
        using var client = new HttpClient { BaseAddress = baseAddress };
        using var response = await client.GetAsync(new Uri(pathAndQuery, UriKind.Relative), TestContext.Current.CancellationToken);

        // 앱마다 포트(0 → 임의)가 달라 server.port로 이 테스트의 서버 span만 고른다. span은 응답을 보낸 뒤 끝나므로 잠시 기다린다.
        bool IsThisServer(Activity activity) => Equals(activity.GetTagItem("server.port"), baseAddress.Port);
        for (var attempt = 0; attempt < 100 && !_stopped.Any(IsThisServer); attempt++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        return _stopped.Where(IsThisServer).Should().ContainSingle().Subject;
    }

    private static async Task<WebApplication> StartAsync(Action<WebApplication> map, Action<WebApplication>? configure = null)
    {
        var builder = TestHosts.CreateWebBuilder("Testing");
        builder.AddServiceDefaults();

        var app = builder.Build();
        configure?.Invoke(app);
        app.UseRouting();
        map(app);

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
