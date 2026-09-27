using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Acceptance;

// 공개 진입점 2: UseBuildingBlocksApi(ADR-0024). 실제 WebApplication 파이프라인(ExceptionHandlerMiddleware + 전역 예외 처리기,
// Swagger 미들웨어)을 서버 없이 RequestDelegate로 실행한다. HTTP 전 구간(Kestrel · 라우팅 · Controller 실행)은 S03-T05.
// 범위 구분: 처리기 규칙 하나하나는 GlobalExceptionHandlerTests, 여기서는 등록 · 파이프라인 결합만 본다.
public sealed class ApiPipelineAcceptanceTests
{
    private const string ThrowPath = "/throw";
    private const string SwaggerPath = "/swagger/v1/swagger.json";

    private readonly FakeLogCollector _logs = new();

    // ---- 성공 ----

    [Fact]
    public async Task UnhandledException_ThroughPipeline_Responds9001AndOnlyGlobalHandlerLogsEventId1()
    {
        await using var app = CreateApp("Production");
        var context = await InvokeAsync(app, ThrowPath);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().StartWith(ErrorProblemDetails.ContentType);
        HttpContexts.ReadJson(context).GetProperty("code").GetInt32().Should().Be(9001);

        // 프레임워크 ExceptionHandlerMiddleware의 자체 Error 로그(이벤트 ID 1, 원본 메시지 포함)는 걸러진다.
        var errors = _logs.GetSnapshot().Where(record => record.Level >= LogLevel.Error).ToList();
        errors.Should().ContainSingle();
        errors[0].Id.Id.Should().Be(1);
        errors[0].Category.Should().Be("EmergencyHub.BuildingBlocks.Api.Exceptions.GlobalExceptionHandler");
        _logs.GetSnapshot().Should().NotContain(record => record.Message.Contains(UnconvertedDbFailures.CheckConstraintName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Development_ServesOpenApiDocumentV1WithIntegerEnumsAndProblemDetailsExtensions()
    {
        await using var app = CreateApp("Development");
        var context = await InvokeAsync(app, SwaggerPath);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        var document = HttpContexts.ReadJson(context);
        document.GetProperty("info").GetProperty("title").GetString().Should().Be("Sample API");
        var schemas = document.GetProperty("components").GetProperty("schemas");
        var status = schemas.GetProperty(nameof(SampleStatus));
        status.GetProperty("type").GetString().Should().Be("integer");
        status.GetProperty("description").GetString().Should().Be("0 = Unknown, 1 = Active, 3 = Retired");
        status.GetProperty("enum").EnumerateArray().Select(value => value.GetInt32()).Should().Equal(0, 1, 3);
        var problem = schemas.GetProperty("ProblemDetails").GetProperty("properties");
        problem.GetProperty("code").GetProperty("type").GetString().Should().Be("integer");
        problem.GetProperty("traceId").GetProperty("type").GetString().Should().Be("string");
        problem.TryGetProperty("errors", out _).Should().BeTrue();
    }

    // ---- 실패 ----

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task NonDevelopment_DoesNotServeOpenApiDocument(string environment)
    {
        await using var app = CreateApp(environment);
        var context = await InvokeAsync(app, SwaggerPath);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        HttpContexts.ReadBody(context).Should().NotContain("openapi");
    }

    [Fact]
    public void UseBuildingBlocksApi_NullApp_ThrowsArgumentNullException()
    {
        WebApplication app = null!;

        var act = () => app.UseBuildingBlocksApi();

        act.Should().Throw<ArgumentNullException>().WithParameterName("app");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task UnhandledException_DbMessage_IsNotInResponseOrAnyLog()
    {
        await using var app = CreateApp("Production", UnconvertedDbFailures.CheckViolation);
        var context = await InvokeAsync(app, ThrowPath);

        var body = HttpContexts.ReadBody(context);
        var logs = _logs.GetSnapshot();
        foreach (var fragment in UnconvertedDbFailures.SensitiveFragments)
        {
            body.Should().NotContain(fragment);
            logs.Should().NotContain(record =>
                record.Message.Contains(fragment, StringComparison.Ordinal)
                || (record.Exception != null && record.Exception.ToString().Contains(fragment, StringComparison.Ordinal)));
        }
    }

    [Fact]
    public async Task ReturnsSameApplication()
    {
        await using var app = CreateBuilder("Production").Build();

        app.UseBuildingBlocksApi().Should().BeSameAs(app);
    }

    private WebApplicationBuilder CreateBuilder(string environment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddProvider(new FakeLoggerProvider(_logs));
        builder.Services.AddBuildingBlocksApi("Sample API");
        return builder;
    }

    private WebApplication CreateApp(string environment, Func<Exception>? exceptionFactory = null)
    {
        var app = CreateBuilder(environment).Build();
        app.UseBuildingBlocksApi();
        app.Run(context =>
        {
            if (context.Request.Path == ThrowPath)
            {
                throw (exceptionFactory ?? (() => new InvalidOperationException("boom")))();
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        return app;
    }

    private static async Task<HttpContext> InvokeAsync(WebApplication app, string path)
    {
        var pipeline = ((IApplicationBuilder)app).Build();
        await using var scope = app.Services.CreateAsyncScope();
        var context = HttpContexts.Create(path, scope.ServiceProvider);
        context.Request.Method = HttpMethods.Get;
        await pipeline(context);
        return context;
    }
}
