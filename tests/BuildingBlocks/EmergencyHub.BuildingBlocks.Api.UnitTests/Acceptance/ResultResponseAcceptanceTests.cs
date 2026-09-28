using System.Text.Json;
using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Api.UnitTests.Routing;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Acceptance;

// PRD-001 FR-07 인수 조건 "실패 Result, 바인딩 오류, 정의되지 않은 enum 입력이 api-guidelines · error-codes 형식(code 포함)으로 응답된다".
// 서비스 Api 호스트와 같은 조립(AddBuildingBlocksInfrastructure → AddConventionalServices → AddBuildingBlocksApi)으로 만들고,
// MVC 출력 포맷터(ObjectResult 실행기, System.Text.Json 웹 기본값)로 실제 응답 JSON까지 확인한다.
// 규칙 하나하나는 Errors · Validation 단위 테스트가 보고, 여기서는 FR 시나리오 단위로 결합만 본다. HTTP 전 구간(Kestrel · 라우팅 · Controller)은 S03-T05.
[Trait("FR", "PRD-001/FR-07")]
public sealed class ResultResponseAcceptanceTests
{
    // S07-T02(ADR-0025): instance는 요청 경로가 아니라 라우트 템플릿이다. 요청 경로의 경로 매개변수 값(7)이 응답에 없다.
    private const string RequestPath = "/api/v1/employees/7";
    private const string RouteTemplate = "api/v1/employees/{id}";

    // ErrorType마다 대표 오류 하나(None 제외 전수). 공통 코드가 없는 BusinessRule만 서비스 코드 예시(24001)를 쓴다.
    private static readonly Error[] OneErrorPerType =
    [
        CommonErrors.ValidationFailed,
        CommonErrors.PayloadTooLarge,
        CommonErrors.UnsupportedMediaType,
        CommonErrors.NotFound,
        CommonErrors.ConcurrencyConflict,
        SampleErrors.InvalidTransition,
        CommonErrors.Unauthenticated,
        CommonErrors.Forbidden,
        CommonErrors.Unexpected,
        CommonErrors.ExternalServiceFailed,
        CommonErrors.TemporarilyUnavailable,
    ];

    public static TheoryData<Error, int, string> ErrorTypeResponses() => new()
    {
        { CommonErrors.ValidationFailed, 400, "Bad Request" },
        { CommonErrors.PayloadTooLarge, 413, "Payload Too Large" },
        { CommonErrors.UnsupportedMediaType, 415, "Unsupported Media Type" },
        { CommonErrors.NotFound, 404, "Not Found" },
        { CommonErrors.ConcurrencyConflict, 409, "Conflict" },
        { SampleErrors.InvalidTransition, 422, "Unprocessable Entity" },
        { CommonErrors.Unauthenticated, 401, "Unauthorized" },
        { CommonErrors.Forbidden, 403, "Forbidden" },
        { CommonErrors.Unexpected, 500, "Internal Server Error" },
        { CommonErrors.ExternalServiceFailed, 502, "Bad Gateway" },
        { CommonErrors.TemporarilyUnavailable, 503, "Service Unavailable" },
    };

    // ---- 성공: 실패 Result → ProblemDetails ----

    [Theory]
    [MemberData(nameof(ErrorTypeResponses))]
    public async Task FailureResult_EachErrorType_RespondsProblemJsonWithNumericCodeAndTraceId(Error error, int status, string title)
    {
        await using var app = Compose();
        var context = CreateActionContext(app.Services);

        await Result.Failure(error).Error.ToProblemResult().ExecuteResultAsync(context);

        var response = context.HttpContext.Response;
        response.StatusCode.Should().Be(status);
        response.ContentType.Should().StartWith(ErrorProblemDetails.ContentType);
        var json = HttpContexts.ReadJson(context.HttpContext);
        json.GetProperty("type").GetString().Should().Be($"https://httpstatuses.io/{status}");
        json.GetProperty("title").GetString().Should().Be(title);
        json.GetProperty("status").GetInt32().Should().Be(status);
        json.GetProperty("detail").GetString().Should().Be(error.Message);
        json.GetProperty("instance").GetString().Should().Be("/" + RouteTemplate);
        json.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number, "ADR-0008: 에러 코드는 문자열이 아니라 정수");
        json.GetProperty("code").GetInt32().Should().Be(error.Code);
        json.GetProperty("traceId").GetString().Should().Be(HttpContexts.TraceIdentifier, "Activity가 없으면 TraceIdentifier");
        json.TryGetProperty("errors", out _).Should().BeFalse();
    }

    [Fact]
    public void ErrorTypeResponses_CoverEveryDefinedErrorTypeExceptNone()
    {
        // 공허 통과 방지: ErrorType이 늘면 위 Theory 데이터도 늘려야 한다.
        OneErrorPerType.Select(error => error.Type).Should().BeEquivalentTo(Enum.GetValues<ErrorType>().Where(type => type != ErrorType.None));
    }

    // ---- 성공: 정의된 코드값은 통과 ----

    [Theory]
    [InlineData(SampleStatus.Active, SampleChannels.None)]
    [InlineData(SampleStatus.Active, SampleChannels.Sms)]
    [InlineData(SampleStatus.Retired, SampleChannels.Sms | SampleChannels.Email)]
    [InlineData(SampleStatus.Retired, SampleChannels.All)]
    public async Task DefinedCodes_ThroughValidationPipeline_ReachHandler(SampleStatus status, SampleChannels channels)
    {
        var result = await QueryAsync(new ChannelFilterQuery(status, channels));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new SampleResponse(status, channels, Note: null));
    }

    // ---- 실패: 정의되지 않은 enum 입력 → 400, 1001 + 필드 1002 ----

    [Theory]
    [InlineData((short)2)] // 1과 3 사이의 빈 값
    [InlineData((short)0)] // 예약 값 Unknown(정의되어 있어도 거부)
    [InlineData((short)-1)]
    [InlineData(short.MaxValue)]
    public async Task UndefinedCode_ThroughValidationPipeline_Responds400With1001AndField1002(short status)
    {
        var json = await ToProblemJsonAsync(new ChannelFilterQuery((SampleStatus)status, SampleChannels.Sms));

        AssertInvalidCodeFields(json, "status");
    }

    [Theory]
    [InlineData(1 << 3)] // 정의되지 않은 비트 하나
    [InlineData((1 << 0) | (1 << 3))] // 정의된 비트 + 정의되지 않은 비트
    [InlineData(-1)] // 모든 비트
    [InlineData(int.MinValue)] // 부호 비트
    public async Task UndefinedFlagBits_ThroughValidationPipeline_Responds400With1001AndField1002(int channels)
    {
        var json = await ToProblemJsonAsync(new ChannelFilterQuery(SampleStatus.Active, (SampleChannels)channels));

        AssertInvalidCodeFields(json, "channels");
    }

    [Fact]
    public async Task UndefinedCodeAndFlags_Together_ReportsBothFieldsInRuleOrder()
    {
        var json = await ToProblemJsonAsync(new ChannelFilterQuery(SampleStatus.Unknown, (SampleChannels)(1 << 4)));

        AssertInvalidCodeFields(json, "status", "channels");
    }

    // ---- 실패: 바인딩 오류 → 400, 1001 ----

    [Fact]
    public async Task BindingErrors_ThroughConfiguredFactory_Responds400With1001PerFieldWithoutInputValues()
    {
        // System.Text.Json 입력 포맷터가 남기는 모델 상태 키 형식($ 접두사, 인덱서, 중첩)과 입력 값이 든 프레임워크 메시지.
        await using var app = Compose();
        var context = CreateActionContext(app.Services);
        context.ModelState.AddModelError("$.items[0].name", "The JSON value 'hong@example.com' could not be converted to System.String.");
        context.ModelState.AddModelError("$.address.zipCode", "The JSON value could not be converted to System.Int32. Path: $.address.zipCode");
        context.ModelState.AddModelError("$", "'{' is invalid after a single JSON value. LineNumber: 0 | BytePositionInLine: 12.");
        var factory = app.Services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value.InvalidModelStateResponseFactory;

        await factory(context).ExecuteResultAsync(context);

        context.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.HttpContext.Response.ContentType.Should().StartWith(ErrorProblemDetails.ContentType);
        var body = HttpContexts.ReadBody(context.HttpContext);
        var json = HttpContexts.ReadJson(context.HttpContext);
        json.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number);
        json.GetProperty("code").GetInt32().Should().Be(1001);
        json.GetProperty("traceId").GetString().Should().Be(HttpContexts.TraceIdentifier);
        var errors = json.GetProperty("errors");
        // 키 순서는 ModelStateDictionary 열거 순서(접두사 트리)를 따르므로 집합으로 본다.
        errors.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("items[0].name", "address.zipCode", string.Empty);
        foreach (var field in errors.EnumerateObject())
        {
            var item = field.Value.EnumerateArray().Should().ContainSingle().Subject;
            item.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number);
            item.GetProperty("code").GetInt32().Should().Be(1001);
            item.GetProperty("message").GetString().Should().Be(CommonErrors.ValidationFailed.Message);
        }

        body.Should().NotContain("hong@example.com").And.NotContain("System.").And.NotContain("LineNumber");
    }

    // ---- 엣지: 서비스 호스트 조립 ----

    [Fact]
    public async Task ServiceHostComposition_PassesStrictValidationWithSingleSenderAndExceptionHandler()
    {
        // CreateBuilder가 ValidateOnBuild · ValidateScopes를 켠다. Build()가 예외 없이 끝나면 조립 검증을 통과한 것이다.
        var builder = CreateBuilder();
        var services = builder.Services;
        await using var app = builder.Build();
        await using var scope = app.Services.CreateAsyncScope();

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(ISender));
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IExceptionHandler));
        scope.ServiceProvider.GetRequiredService<IQueryHandler<ChannelFilterQuery, SampleResponse>>()
            .Should().NotBeOfType<ChannelFilterQueryHandler>("검증 데코레이터가 Handler를 감싸야 1002가 응답까지 간다");
    }

    private static void AssertInvalidCodeFields(JsonElement json, params string[] expectedKeys)
    {
        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status400BadRequest);
        json.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.ValidationFailed.Code, "요청 전체는 1001, 필드별 코드가 1002");
        var errors = json.GetProperty("errors");
        errors.EnumerateObject().Select(property => property.Name).Should().Equal(expectedKeys);
        foreach (var field in errors.EnumerateObject())
        {
            var item = field.Value.EnumerateArray().Should().ContainSingle().Subject;
            item.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number);
            item.GetProperty("code").GetInt32().Should().Be(CommonErrors.InvalidCode.Code);
            item.GetProperty("message").GetString().Should().Be(CommonErrors.InvalidCode.Message);
        }
    }

    private static async Task<Result<SampleResponse>> QueryAsync(ChannelFilterQuery query)
    {
        await using var app = Compose();
        await using var scope = app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().QueryAsync(query, TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> ToProblemJsonAsync(ChannelFilterQuery query)
    {
        await using var app = Compose();
        await using var scope = app.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().QueryAsync(query, TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();

        var context = CreateActionContext(scope.ServiceProvider);
        await result.Error.ToProblemResult().ExecuteResultAsync(context);

        context.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.HttpContext.Response.ContentType.Should().StartWith(ErrorProblemDetails.ContentType);
        return HttpContexts.ReadJson(context.HttpContext);
    }

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
        builder.Logging.ClearProviders();
        builder.Services.AddBuildingBlocksInfrastructure();
        builder.Services.AddConventionalServices(typeof(ChannelFilterQuery).Assembly);
        builder.Services.AddBuildingBlocksApi("Sample API");
        return builder;
    }

    private static WebApplication Compose() => CreateBuilder().Build();

    private static ActionContext CreateActionContext(IServiceProvider services)
    {
        var httpContext = HttpContexts.Create(RequestPath, services);
        httpContext.SetEndpoint(RouteTemplatePathTests.RouteEndpointOf(RouteTemplate));
        return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }
}
