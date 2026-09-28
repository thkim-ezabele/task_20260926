using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Errors;

// ADR-0028 "[Consumes] 불일치 415": 엔드포인트 라우팅(ConsumesMatcherPolicy)이 본문 없이 돌려주는 415에 1005 ProblemDetails를 쓴다.
// 415가 아닌 본문 없는 상태 코드 응답은 건드리지 않는다(기존대로 본문 없음).
public sealed class UnsupportedMediaTypeStatusCodeResponsesTests
{
    // ---- 성공 ----

    [Fact]
    public async Task WriteAsync_Empty415_Writes1005ProblemDetails()
    {
        var context = HttpContexts.Create("/api/employee");
        context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;

        await UnsupportedMediaTypeStatusCodeResponses.WriteAsync(CreateStatusCodeContext(context));

        context.Response.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
        context.Response.ContentType.Should().StartWith(ErrorProblemDetails.ContentType);
        var json = HttpContexts.ReadJson(context);
        json.GetProperty("status").GetInt32().Should().Be(415);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.UnsupportedMediaType.Code);
        json.GetProperty("instance").GetString().Should().Be("/api/employee");
        json.GetProperty("traceId").GetString().Should().Be(HttpContexts.TraceIdentifier);
    }

    // ---- 실패 ----

    [Fact]
    public async Task WriteAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = () => UnsupportedMediaTypeStatusCodeResponses.WriteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    // ---- 엣지: 415가 아니면 본문을 쓰지 않음 ----

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status405MethodNotAllowed)]
    [InlineData(StatusCodes.Status413PayloadTooLarge)]
    public async Task WriteAsync_OtherStatus_WritesNothing(int status)
    {
        var context = HttpContexts.Create();
        context.Response.StatusCode = status;

        await UnsupportedMediaTypeStatusCodeResponses.WriteAsync(CreateStatusCodeContext(context));

        context.Response.StatusCode.Should().Be(status);
        context.Response.Body.Length.Should().Be(0);
        context.Response.ContentType.Should().BeNull();
    }

    private static StatusCodeContext CreateStatusCodeContext(HttpContext context) =>
        new(context, new StatusCodePagesOptions(), _ => Task.CompletedTask);
}
