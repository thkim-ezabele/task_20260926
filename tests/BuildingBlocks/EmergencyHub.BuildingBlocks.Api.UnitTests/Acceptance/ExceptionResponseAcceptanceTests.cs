using System.Text.Json;
using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Acceptance;

// PRD-001 FR-07 인수 조건 "처리되지 않은 예외가 api-guidelines · error-codes 형식(code 포함)으로 응답된다".
// 서비스 Api 호스트와 같은 조립(AddBuildingBlocksInfrastructure → AddBuildingBlocksApi → UseBuildingBlocksApi)에
// 실제 영속성 분류기와 실제 EF Core · Npgsql 예외를 흘려, 파이프라인(ExceptionHandlerMiddleware + 전역 예외 처리기) 결과를 확인한다.
// S02-T07 규칙표의 재전파 예외(23514 · 25006 · 재시도 한도 초과)가 HTTP 응답에서 어떻게 보이는지가 대상이다. HTTP 전 구간은 S03-T05.
[Trait("FR", "PRD-001/FR-07")]
public sealed class ExceptionResponseAcceptanceTests
{
    private const string ThrowPath = "/api/v1/employees";

    private readonly FakeLogCollector _logs = new();

    public static TheoryData<string> UnconvertedPostgresFailures() => new(nameof(PostgresFailures.CheckViolation), nameof(PostgresFailures.ReadOnlyViolation));

    // ---- 성공: 분류된 예외 ----

    [Fact]
    public async Task RetryLimitExceeded_RealClassifier_Responds503With9003AndWarningOnly()
    {
        await using var app = CreateApp(PostgresFailures.RetryLimitExceeded);

        var context = await InvokeAsync(app);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        var json = ReadProblem(context);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.TemporarilyUnavailable.Code);
        json.GetProperty("detail").GetString().Should().Be(CommonErrors.TemporarilyUnavailable.Message);
        var logs = _logs.GetSnapshot();
        logs.Should().NotContain(record => record.Level >= LogLevel.Error);
        logs.Should().ContainSingle(record => record.Id.Id == 301).Which.Level.Should().Be(LogLevel.Warning);
        HttpContexts.ReadBody(context).Should().NotContain(UnconvertedDbFailures.TableName).And.NotContain("NpgsqlRetryingExecutionStrategy");
    }

    [Theory]
    [MemberData(nameof(ClassifiedErrors))]
    public async Task ClassifiedException_ThroughPipeline_RespondsClassifiedStatusAndCode(Error error, int status)
    {
        // 404도 그대로 응답한다(AllowStatusCode404Response). 409는 S02-T07 인계(3001 · 3003 · 서비스 23xxx Conflict).
        var classifier = Substitute.For<IExceptionClassifier>();
        classifier.Classify(Arg.Any<Exception>()).Returns(error);
        await using var app = CreateApp(() => new InvalidOperationException("classified"), classifier);

        var context = await InvokeAsync(app);

        context.Response.StatusCode.Should().Be(status);
        var json = ReadProblem(context);
        json.GetProperty("status").GetInt32().Should().Be(status);
        json.GetProperty("code").GetInt32().Should().Be(error.Code);
        json.GetProperty("detail").GetString().Should().Be(error.Message);
    }

    public static TheoryData<Error, int> ClassifiedErrors() => new()
    {
        { CommonErrors.NotFound, StatusCodes.Status404NotFound },
        { CommonErrors.ConcurrencyConflict, StatusCodes.Status409Conflict },
        { CommonErrors.UniqueConstraintViolated, StatusCodes.Status409Conflict },
        { SampleErrors.DuplicateEmail, StatusCodes.Status409Conflict },
    };

    // ---- 실패: 분류되지 않은 예외 → 500, 9001 ----

    [Theory]
    [MemberData(nameof(UnconvertedPostgresFailures))]
    public async Task UnconvertedPostgresException_RealClassifier_Responds9001WithoutSensitiveFragmentsInBodyOrLogs(string failure)
    {
        Func<Exception> factory = failure == nameof(PostgresFailures.CheckViolation) ? PostgresFailures.CheckViolation : PostgresFailures.ReadOnlyViolation;
        await using var app = CreateApp(factory);

        var context = await InvokeAsync(app);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var json = ReadProblem(context);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.Unexpected.Code);
        json.GetProperty("detail").GetString().Should().Be(CommonErrors.Unexpected.Message);
        json.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("type", "title", "status", "detail", "instance", "code", "traceId");

        var body = HttpContexts.ReadBody(context);
        var logs = _logs.GetSnapshot();
        logs.Should().ContainSingle(record => record.Level >= LogLevel.Error).Which.Id.Id.Should().Be(1);
        foreach (var fragment in UnconvertedDbFailures.SensitiveFragments.Append("PostgresException").Append("at EmergencyHub"))
        {
            body.Should().NotContain(fragment);
        }

        foreach (var fragment in UnconvertedDbFailures.SensitiveFragments)
        {
            logs.Should().NotContain(record => LogText(record).Contains(fragment, StringComparison.Ordinal), $"로그에 '{fragment}'가 남으면 안 된다");
        }
    }

    [Fact]
    public async Task BadHttpRequest_ThroughPipeline_Responds400With1001()
    {
        await using var app = CreateApp(() => new BadHttpRequestException("Request body too large. hong@example.com", StatusCodes.Status413PayloadTooLarge));

        var context = await InvokeAsync(app);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var json = ReadProblem(context);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.ValidationFailed.Code);
        HttpContexts.ReadBody(context).Should().NotContain("hong@example.com");
        _logs.GetSnapshot().Should().NotContain(record => record.Level >= LogLevel.Warning);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task UnhandledException_NoActivity_TraceIdIsTraceIdentifierAndInstanceHasNoQuery()
    {
        await using var app = CreateApp(() => new InvalidOperationException("boom"));

        var context = await InvokeAsync(app, "?email=hong@example.com");

        var json = ReadProblem(context);
        json.GetProperty("traceId").GetString().Should().Be(HttpContexts.TraceIdentifier);
        json.GetProperty("instance").GetString().Should().Be(ThrowPath);
        HttpContexts.ReadBody(context).Should().NotContain("hong@example.com");
    }

    private static JsonElement ReadProblem(HttpContext context)
    {
        context.Response.ContentType.Should().StartWith(ErrorProblemDetails.ContentType);
        var json = HttpContexts.ReadJson(context);
        json.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number, "ADR-0008: 에러 코드는 정수");
        json.GetProperty("traceId").ValueKind.Should().Be(JsonValueKind.String);
        return json;
    }

    private static string LogText(FakeLogRecord record)
    {
        var state = record.StructuredState is null ? string.Empty : string.Join('|', record.StructuredState.Select(pair => $"{pair.Key}={pair.Value}"));
        var exception = record.Exception?.ToString() ?? string.Empty;
        var data = record.Exception is null ? string.Empty : string.Join('|', record.Exception.Data.Values.Cast<object?>());
        return string.Join('\n', record.Message, state, exception, data);
    }

    private WebApplication CreateApp(Func<Exception> exceptionFactory, IExceptionClassifier? extraClassifier = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddProvider(new FakeLoggerProvider(_logs));
        builder.Services.AddBuildingBlocksInfrastructure();
        if (extraClassifier is not null)
        {
            builder.Services.AddSingleton(extraClassifier);
        }

        builder.Services.AddBuildingBlocksApi("Sample API");

        var app = builder.Build();
        app.UseBuildingBlocksApi();
        app.Run(_ => throw exceptionFactory());
        return app;
    }

    private static async Task<HttpContext> InvokeAsync(WebApplication app, string queryString = "")
    {
        var pipeline = ((IApplicationBuilder)app).Build();
        await using var scope = app.Services.CreateAsyncScope();
        var context = HttpContexts.Create(ThrowPath, scope.ServiceProvider);
        context.Request.QueryString = new QueryString(queryString);
        await pipeline(context);
        return context;
    }
}
