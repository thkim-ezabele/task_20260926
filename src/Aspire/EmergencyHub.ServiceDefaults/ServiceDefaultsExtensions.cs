using System.Diagnostics;
using EmergencyHub.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Events;

namespace EmergencyHub.ServiceDefaults;

/// <summary>
/// 서비스 호스트(Api · MigrationService) 공통 기본값입니다(ADR-0011, ADR-0020, PRD-001 FR-03).
/// </summary>
/// <remarks>
/// Aspire 9.5.2 ServiceDefaults 템플릿과 다른 점(TD-012, 근거 ADR-0020):
/// <list type="number">
/// <item><description>OpenTelemetry SDK 로그 공급자(<c>builder.Logging.AddOpenTelemetry</c>)를 두지 않습니다. 로그는 Serilog OTLP 싱크 하나로만 나갑니다.</description></item>
/// <item><description><c>UseOtlpExporter()</c>(모든 신호) 대신 트레이스 · 메트릭 각각에 <c>AddOtlpExporter()</c>를 붙입니다.</description></item>
/// <item><description>Npgsql 추적(<c>AddNpgsql</c>)을 더합니다. EF Core OTel 계측은 Npgsql span과 겹쳐 넣지 않습니다.</description></item>
/// <item><description>헬스 경로는 <c>/health/live</c> · <c>/health/ready</c>이고 모든 환경에 매핑합니다(BL-030). <c>self</c> 검사는 두지 않습니다(live는 검사를 실행하지 않음).</description></item>
/// <item><description>요청 완료 로그 <c>RequestPath</c>와 추적 span <c>url.path</c>는 라우트 템플릿이 있으면 템플릿, 없으면 요청 경로입니다(ADR-0025, <see cref="RouteTemplatePath"/>).
/// 요청 안 로그의 호스팅 범위 <c>RequestPath</c>도 같게 바꿉니다(<see cref="RouteTemplateRequestPathEnricher"/>). 경로 매개변수 값(이름 등 개인정보)이 남지 않습니다. <c>PathBase</c>는 붙이지 않습니다.</description></item>
/// </list>
/// </remarks>
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// OpenTelemetry 트레이스 · 메트릭, Serilog, 헬스체크 서비스, 서비스 디스커버리, HTTP 복원력을 등록합니다.
    /// </summary>
    /// <typeparam name="TBuilder">호스트 빌더(WebApplicationBuilder · HostApplicationBuilder).</typeparam>
    /// <param name="builder">호스트 빌더.</param>
    /// <returns>같은 <paramref name="builder"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/>가 <see langword="null"/>인 경우.</exception>
    /// <remarks>ASP.NET Core 없이(Worker) 불러도 동작합니다. HTTP 엔드포인트는 <see cref="MapDefaultEndpoints"/>가 따로 매핑합니다.</remarks>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureOpenTelemetry();
        builder.ConfigureSerilog();

        builder.Services.AddHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// 헬스 엔드포인트 <c>/health/live</c>(검사 없음) · <c>/health/ready</c>(<see cref="HealthEndpoints.ReadyTag"/> 검사만)를 모든 환경에 매핑합니다.
    /// </summary>
    /// <param name="app">웹 애플리케이션.</param>
    /// <returns>같은 <paramref name="app"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/>이 <see langword="null"/>인 경우.</exception>
    /// <remarks>응답 본문은 기본 작성기의 상태 문자열뿐이라 검사 이름 · 설명 · 예외가 나가지 않습니다.</remarks>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(HealthEndpoints.LivePath, new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks(HealthEndpoints.ReadyPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthEndpoints.ReadyTag),
        });

        return app;
    }

    /// <summary>추적 span의 경로 속성 이름입니다(OpenTelemetry 의미 규약 <c>url.path</c>).</summary>
    internal const string UrlPathTag = "url.path";

    /// <summary>
    /// 요청 완료 로그의 메시지 템플릿 속성입니다. Serilog.AspNetCore 기본값(<c>RequestMethod</c> · <c>RequestPath</c> · <c>StatusCode</c> · <c>Elapsed</c>)과 같고,
    /// <c>RequestPath</c>만 라우트 템플릿이 있으면 템플릿으로 바꿉니다(ADR-0025).
    /// </summary>
    /// <param name="httpContext">요청 컨텍스트.</param>
    /// <param name="requestPath">미들웨어가 계산한 요청 경로(쿼리 문자열 없음). 템플릿이 없을 때 씁니다.</param>
    /// <param name="elapsedMilliseconds">처리 시간(ms).</param>
    /// <param name="statusCode">응답 상태 코드.</param>
    /// <returns>메시지 템플릿 속성 4개.</returns>
    internal static IEnumerable<LogEventProperty> GetRequestLogProperties(HttpContext httpContext, string requestPath, double elapsedMilliseconds, int statusCode) =>
    [
        new LogEventProperty("RequestMethod", new ScalarValue(httpContext.Request.Method)),
        new LogEventProperty("RequestPath", new ScalarValue(RouteTemplatePath.Find(httpContext) ?? requestPath)),
        new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
        new LogEventProperty("Elapsed", new ScalarValue(elapsedMilliseconds)),
    ];

    /// <summary>
    /// 라우트 템플릿이 있으면 span의 <c>url.path</c>(계측이 요청 시작 때 넣은 요청 경로)를 템플릿으로 덮어씁니다(ADR-0025). 없으면 그대로 둡니다.
    /// </summary>
    /// <param name="activity">요청 span.</param>
    /// <param name="response">응답(계측 <c>EnrichWithHttpResponse</c>, span 종료 때 호출).</param>
    internal static void ReplaceUrlPathWithRouteTemplate(Activity activity, HttpResponse response)
    {
        if (RouteTemplatePath.Find(response.HttpContext) is { } template)
        {
            activity.SetTag(UrlPathTag, template);
        }
    }

    private static void ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var exportOtlp = OtlpEndpoint.IsConfigured(builder.Configuration);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (exportOtlp)
                {
                    metrics.AddOtlpExporter();
                }
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context => !HealthEndpoints.IsHealthPath(context.Request.Path);
                        options.EnrichWithHttpResponse = ReplaceUrlPathWithRouteTemplate;
                    })
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (exportOtlp)
                {
                    tracing.AddOtlpExporter();
                }
            });
    }

    private static void ConfigureSerilog<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        // 요청 안 로그의 호스팅 범위 RequestPath(요청 경로)를 라우트 템플릿으로 덮는다(ADR-0025, S07-T02 실측). ReadFrom.Services가 보강기로 붙인다.
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<ILogEventEnricher>(services => new RouteTemplateRequestPathEnricher(services.GetRequiredService<IHttpContextAccessor>()));

        // 정적 Log · 부트스트랩 로거는 쓰지 않는다(ADR-0020). 정적 Log.Logger를 바꾸지 않아 호스트마다 로거가 독립이다.
        builder.Services.AddSerilog(
            (services, logger) => SerilogDefaults.Configure(logger, services, configuration, environment),
            preserveStaticLogger: true,
            writeToProviders: false);

        // UseSerilogRequestLogging은 options.Logger가 없으면 정적 Log에 쓴다. 정적 로거를 바꾸지 않으므로(위) 요청 완료 로그가 사라진다
        // (S03-T05 발견, S03-T07 수정). 미들웨어가 읽는 IOptions<RequestLoggingOptions>에 DI의 Serilog 로거를 넣어 모든 서비스에 한 번에 적용한다.
        // 호출 쪽 configureOptions는 이 값 위에 적용되므로 Logger를 직접 주면 그 값이 이긴다.
        // RequestPath는 메시지 템플릿 속성 자체를 라우트 템플릿으로 바꾼다(ADR-0025). EnrichDiagnosticContext로 속성을 더하는 방식은 쓰지 않는다(S07 계획 실측).
        builder.Services.AddOptions<RequestLoggingOptions>()
            .Configure<Serilog.ILogger>((options, logger) =>
            {
                options.Logger = logger;
                options.GetMessageTemplateProperties = GetRequestLogProperties;
            });
    }
}
