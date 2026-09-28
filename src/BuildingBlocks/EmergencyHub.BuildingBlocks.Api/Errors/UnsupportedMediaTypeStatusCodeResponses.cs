using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace EmergencyHub.BuildingBlocks.Api.Errors;

/// <summary>
/// 본문 없이 끝난 <c>415</c> 응답에 공통 <c>ProblemDetails</c>(<c>415</c> · <c>1005</c>)를 씁니다(ADR-0028 "[Consumes] 불일치 415").
/// <c>UseStatusCodePages</c> 처리기로 씁니다.
/// </summary>
/// <remarks>
/// <para>
/// 경로(S06-T01 실측): <c>[Consumes]</c>에 없는 Content-Type 요청은 엔드포인트 라우팅(<c>ConsumesMatcherPolicy</c>)이 액션을 고르지 않고
/// 본문 없는 <c>415</c>로 끝냅니다. MVC 필터(<c>ClientErrorResultFilter</c>)를 지나지 않으므로 상태 코드 페이지 단계에서 바꿉니다.
/// </para>
/// <para>
/// <c>415</c>가 아닌 상태 코드는 아무것도 쓰지 않습니다(본문 없는 응답은 기존대로). 상태 코드 페이지 미들웨어는 본문 · Content-Type이
/// 이미 있는 응답에는 처리기를 부르지 않으므로, MVC가 만든 <c>ProblemDetails</c> 응답은 바뀌지 않습니다.
/// </para>
/// </remarks>
internal static class UnsupportedMediaTypeStatusCodeResponses
{
    /// <summary>응답 상태가 <c>415</c>면 1005 <c>ProblemDetails</c>를 씁니다.</summary>
    /// <param name="context">상태 코드 페이지 컨텍스트.</param>
    /// <returns>쓰기 작업.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/>가 <see langword="null"/>인 경우.</exception>
    public static Task WriteAsync(StatusCodeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.HttpContext;
        if (httpContext.Response.StatusCode != StatusCodes.Status415UnsupportedMediaType)
        {
            return Task.CompletedTask;
        }

        var options = httpContext.RequestServices?.GetService<IOptions<HttpJsonOptions>>()?.Value ?? new HttpJsonOptions();
        var problem = ErrorProblemDetails.Create(CommonErrors.UnsupportedMediaType, httpContext);
        return httpContext.Response.WriteAsJsonAsync(problem, options.SerializerOptions, ErrorProblemDetails.ContentType, httpContext.RequestAborted);
    }
}
