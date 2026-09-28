using EmergencyHub.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace EmergencyHub.ServiceDefaults.UnitTests.Routing;

// S07-T02(ADR-0025 "이름 경로 매개변수와 개인정보"): 요청 경로 대신 쓸 라우트 템플릿을 찾는다.
// 원본은 GetEndpoint() ?? IExceptionHandlerFeature.Endpoint의 RouteEndpoint.RoutePattern.RawText(.NET 8 ExceptionHandlerMiddleware는 처리기 호출 전에
// endpoint를 지움). "/"로 시작하지 않으면 붙이고, 대소문자는 그대로 둔다. 템플릿이 없으면 null(호출 쪽이 요청 경로를 씀).
// BuildingBlocks.Api의 같은 도우미(RouteTemplatePath)와 같은 표다(두 프로젝트는 서로 참조할 수 없어 도우미를 중복, TD).
public sealed class RouteTemplatePathTests
{
    public static TheoryData<string, string> Templates() => new()
    {
        { "api/employee/{name}", "/api/employee/{name}" },
        { "/health/live", "/health/live" },
        { "api/employee", "/api/employee" },
        { "Api/Employee/{Name}", "/Api/Employee/{Name}" },
        { "api/v1/items/{id:int}", "/api/v1/items/{id:int}" },
        { "", "/" },
    };

    // ---- 성공: 엔드포인트의 라우트 템플릿 ----

    [Theory]
    [MemberData(nameof(Templates))]
    public void Find_RouteEndpoint_ReturnsTemplateStartingWithSlash(string rawText, string expected)
    {
        var context = CreateContext("/api/employee/홍길동");
        context.SetEndpoint(RouteEndpointOf(rawText));

        RouteTemplatePath.Find(context).Should().Be(expected);
    }

    [Fact]
    public void Find_EndpointClearedByExceptionHandler_ReturnsTemplateFromExceptionHandlerFeature()
    {
        // 500 경로: ExceptionHandlerMiddleware가 SetEndpoint(null) 뒤 원래 엔드포인트를 IExceptionHandlerFeature.Endpoint에 둔다.
        var context = CreateContext("/api/employee/홍길동");
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new InvalidOperationException("boom"),
            Endpoint = RouteEndpointOf("api/employee/{name}"),
            Path = "/api/employee/홍길동",
        });

        RouteTemplatePath.Find(context).Should().Be("/api/employee/{name}");
    }

    [Fact]
    public void Find_BothPresent_PrefersCurrentEndpoint()
    {
        var context = CreateContext("/x");
        context.SetEndpoint(RouteEndpointOf("current/{id}"));
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new InvalidOperationException("boom"),
            Endpoint = RouteEndpointOf("original/{id}"),
            Path = "/x",
        });

        RouteTemplatePath.Find(context).Should().Be("/current/{id}");
    }

    // ---- 실패: 템플릿이 없으면 null(요청 경로 유지) ----

    [Fact]
    public void Find_NoEndpoint_ReturnsNull()
    {
        // 라우팅 전 오류 · 일치하는 엔드포인트 없음(404 · 405 fallback).
        RouteTemplatePath.Find(CreateContext("/api/employee/홍길동")).Should().BeNull();
    }

    [Fact]
    public void Find_NonRouteEndpoint_ReturnsNull()
    {
        // [Consumes] 불일치 415는 라우팅이 RouteEndpoint가 아닌 엔드포인트("415 HTTP Unsupported Media Type")를 고른다.
        var context = CreateContext("/api/employee");
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "415 HTTP Unsupported Media Type"));

        RouteTemplatePath.Find(context).Should().BeNull();
    }

    [Fact]
    public void Find_NullContext_ThrowsArgumentNullException()
    {
        var act = () => RouteTemplatePath.Find(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpContext");
    }

    // ---- 엣지 ----

    [Fact]
    public void Find_RoutePatternWithoutRawText_ReturnsNull()
    {
        // 세그먼트로 직접 만든 RoutePattern은 RawText가 null이다. 템플릿을 조립하지 않고 요청 경로로 돌아간다.
        var pattern = RoutePatternFactory.Pattern(RoutePatternFactory.Segment(RoutePatternFactory.LiteralPart("items")));
        pattern.RawText.Should().BeNull();
        var context = CreateContext("/items");
        context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, pattern, 0, EndpointMetadataCollection.Empty, "items"));

        RouteTemplatePath.Find(context).Should().BeNull();
    }

    [Fact]
    public void Find_ExceptionHandlerFeatureWithoutEndpoint_ReturnsNull()
    {
        var context = CreateContext("/api/employee/홍길동");
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature { Error = new InvalidOperationException("boom"), Path = "/x" });

        RouteTemplatePath.Find(context).Should().BeNull();
    }

    [Fact]
    public void Find_Template_DoesNotContainRouteValue()
    {
        var context = CreateContext("/api/employee/홍길동");
        context.SetEndpoint(RouteEndpointOf("api/employee/{name}"));
        context.Request.RouteValues["name"] = "홍길동";

        RouteTemplatePath.Find(context).Should().NotContain("홍길동");
    }

    internal static RouteEndpoint RouteEndpointOf(string rawText) =>
        new(_ => Task.CompletedTask, RoutePatternFactory.Parse(rawText), 0, EndpointMetadataCollection.Empty, rawText);

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context;
    }
}
