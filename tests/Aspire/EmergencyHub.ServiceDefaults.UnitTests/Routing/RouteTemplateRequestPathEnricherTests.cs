using EmergencyHub.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace EmergencyHub.ServiceDefaults.UnitTests.Routing;

// S07-T02(ADR-0025, NFR-04): 호스팅 로그 범위의 RequestPath(요청 경로)를 라우트 템플릿으로 덮는 보강기. 템플릿이 없거나 요청 밖이면 그대로 둔다.
public sealed class RouteTemplateRequestPathEnricherTests
{
    // ---- 성공 ----

    [Fact]
    public void Enrich_RouteTemplate_ReplacesRequestPath()
    {
        var logEvent = CreateEvent(("RequestPath", "/api/employee/%ED%99%8D"));

        CreateEnricher(ContextWithTemplate("api/employee/{name}")).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties["RequestPath"].ToString().Should().Be("\"/api/employee/{name}\"");
    }

    // ---- 실패: 바꾸지 않음 ----

    [Fact]
    public void Enrich_NoHttpContext_LeavesEventUntouched()
    {
        var logEvent = CreateEvent(("RequestPath", "/worker"));

        CreateEnricher(httpContext: null).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties["RequestPath"].ToString().Should().Be("\"/worker\"");
    }

    [Fact]
    public void Enrich_NoRouteTemplate_KeepsRequestPath()
    {
        var logEvent = CreateEvent(("RequestPath", "/missing"));

        CreateEnricher(new DefaultHttpContext()).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties["RequestPath"].ToString().Should().Be("\"/missing\"");
    }

    [Fact]
    public void Enrich_NullArguments_Throw()
    {
        var enricher = CreateEnricher(new DefaultHttpContext());

        FluentActions.Invoking(() => enricher.Enrich(null!, new PropertyFactory())).Should().Throw<ArgumentNullException>().WithParameterName("logEvent");
        FluentActions.Invoking(() => enricher.Enrich(CreateEvent(), null!)).Should().Throw<ArgumentNullException>().WithParameterName("propertyFactory");
    }

    // ---- 엣지 ----

    [Fact]
    public void Enrich_EventWithoutRequestPath_KeepsTemplateWhenScopeIsAppliedLater()
    {
        // 범위 속성은 없을 때만 더해지므로(AddPropertyIfAbsent) 먼저 넣은 템플릿이 남는다.
        var logEvent = CreateEvent();

        CreateEnricher(ContextWithTemplate("items/{id}")).Enrich(logEvent, new PropertyFactory());
        logEvent.AddPropertyIfAbsent(new LogEventProperty("RequestPath", new ScalarValue("/items/secret")));

        logEvent.Properties["RequestPath"].ToString().Should().Be("\"/items/{id}\"");
    }

    [Fact]
    public void Enrich_ExceptionHandlerPath_UsesTemplateFromFeature()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new InvalidOperationException("boom"),
            Endpoint = RouteTemplatePathTests.RouteEndpointOf("api/employee/{name}"),
            Path = "/api/employee/secret",
        });
        var logEvent = CreateEvent(("RequestPath", "/api/employee/secret"));

        CreateEnricher(context).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties["RequestPath"].ToString().Should().Be("\"/api/employee/{name}\"");
    }

    private static DefaultHttpContext ContextWithTemplate(string rawText)
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(RouteTemplatePathTests.RouteEndpointOf(rawText));
        return context;
    }

    private static RouteTemplateRequestPathEnricher CreateEnricher(HttpContext? httpContext)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return new RouteTemplateRequestPathEnricher(accessor);
    }

    private static LogEvent CreateEvent(params (string Name, string Value)[] properties) =>
        new(
            DateTimeOffset.UnixEpoch,
            LogEventLevel.Warning,
            exception: null,
            new MessageTemplateParser().Parse("sample"),
            properties.Select(property => new LogEventProperty(property.Name, new ScalarValue(property.Value))));

    private sealed class PropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) => new(name, new ScalarValue(value));
    }
}
