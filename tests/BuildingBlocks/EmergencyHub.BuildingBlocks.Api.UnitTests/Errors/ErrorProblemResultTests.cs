using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Errors;

// Controller가 실패 Result를 돌려줄 때 쓰는 ActionResult(ADR-0016 "Result → ProblemDetails 공통 변환기").
// MVC 출력 포맷터로 실제 직렬화까지 확인한다(HTTP 전 구간은 S03-T05).
public sealed class ErrorProblemResultTests
{
    // ---- 성공 ----

    [Fact]
    public async Task ExecuteResultAsync_ConflictError_Writes409ProblemJsonWithIntegerCode()
    {
        await using var services = CreateServices();
        var context = CreateActionContext(services);

        await SampleErrors.DuplicateEmail.ToProblemResult().ExecuteResultAsync(context);

        context.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.HttpContext.Response.ContentType.Should().StartWith(ErrorProblemDetails.ContentType);
        var json = HttpContexts.ReadJson(context.HttpContext);
        json.GetProperty("code").GetInt32().Should().Be(23001);
        json.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteResultAsync_ValidationError_WritesErrorsWithCamelCaseKeys()
    {
        await using var services = CreateServices();
        var context = CreateActionContext(services);
        var error = ValidationError.Create([FieldError.Create("Email", SampleErrors.InvalidEmail)]);

        await new ErrorProblemResult(error).ExecuteResultAsync(context);

        context.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var json = HttpContexts.ReadJson(context.HttpContext);
        json.GetProperty("errors").GetProperty("email")[0].GetProperty("code").GetInt32().Should().Be(21001);
    }

    [Fact]
    public void Constructor_KeepsError()
    {
        new ErrorProblemResult(CommonErrors.Forbidden).Error.Should().BeSameAs(CommonErrors.Forbidden);
    }

    // ---- 실패 ----

    [Fact]
    public void Constructor_NullError_ThrowsArgumentNullException()
    {
        var act = () => new ErrorProblemResult(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    [Fact]
    public void ToProblemResult_NullError_ThrowsArgumentNullException()
    {
        Error error = null!;

        var act = () => error.ToProblemResult();

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    [Fact]
    public async Task ExecuteResultAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = () => new ErrorProblemResult(CommonErrors.NotFound).ExecuteResultAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task ExecuteResultAsync_UndefinedErrorTypeIsImpossible_InternalErrorWrites500()
    {
        // Error는 None을 가질 수 없으므로(생성 시 검증) 서버 오류 경로는 Internal(9001)로 확인한다.
        await using var services = CreateServices();
        var context = CreateActionContext(services);

        await CommonErrors.Unexpected.ToProblemResult().ExecuteResultAsync(context);

        context.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        HttpContexts.ReadJson(context.HttpContext).GetProperty("code").GetInt32().Should().Be(9001);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocksApi("Test API");
        return services.BuildServiceProvider();
    }

    private static ActionContext CreateActionContext(IServiceProvider services) =>
        new(HttpContexts.Create(services: services), new RouteData(), new ActionDescriptor());
}
