using System.Diagnostics;
using System.Globalization;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace EmergencyHub.BuildingBlocks.Api.Errors;

/// <summary>
/// <see cref="Error"/>를 RFC 9457 <see cref="ProblemDetails"/>로 바꿉니다(원본: wiki/04-development/api-guidelines.md "에러 응답 포맷",
/// ADR-0016, ADR-0024). <c>Result</c> 실패 응답 · 바인딩 오류 · 전역 예외 처리가 모두 이 변환을 씁니다.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>status</c>: <see cref="ErrorStatusCodes.ToStatusCode"/>. <c>type</c>: <c>https://httpstatuses.io/{status}</c>. <c>title</c>: 상태 코드의 표준 문구.</description></item>
/// <item><description><c>detail</c>: <see cref="Error.Message"/>. <c>instance</c>: 요청 경로(<c>PathBase + Path</c>). 쿼리 문자열은 개인정보가 들어갈 수 있어 넣지 않습니다.</description></item>
/// <item><description>확장 <c>code</c>(정수, JSON 숫자)와 <c>traceId</c>는 항상 넣습니다.</description></item>
/// <item><description><see cref="ValidationError"/>면 확장 <c>errors</c>에 필드별 상세를 camelCase 키로 묶어 넣습니다(키 · 항목 모두 생성 순서 유지).</description></item>
/// </list>
/// </remarks>
public static class ErrorProblemDetails
{
    /// <summary>응답 콘텐츠 형식입니다.</summary>
    public const string ContentType = "application/problem+json";

    /// <summary>정수 에러 코드 확장 필드 이름입니다.</summary>
    public const string CodeExtension = "code";

    /// <summary>추적 ID 확장 필드 이름입니다.</summary>
    public const string TraceIdExtension = "traceId";

    /// <summary>필드별 검증 오류 확장 필드 이름입니다.</summary>
    public const string ErrorsExtension = "errors";

    private const string TypeBaseUri = "https://httpstatuses.io/";

    /// <summary>오류와 현재 요청으로 <see cref="ProblemDetails"/>를 만듭니다.</summary>
    /// <param name="error">실패 원인.</param>
    /// <param name="httpContext">현재 요청.</param>
    /// <returns>만든 <see cref="ProblemDetails"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> 또는 <paramref name="httpContext"/>가 <see langword="null"/>인 경우.</exception>
    public static ProblemDetails Create(Error error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        var status = error.Type.ToStatusCode();
        var problem = new ProblemDetails
        {
            Type = TypeBaseUri + status.ToString(CultureInfo.InvariantCulture),
            Title = ReasonPhrases.GetReasonPhrase(status),
            Status = status,
            Detail = error.Message,
            Instance = (httpContext.Request.PathBase + httpContext.Request.Path).Value,
        };

        problem.Extensions[CodeExtension] = error.Code;
        problem.Extensions[TraceIdExtension] = GetTraceId(httpContext);
        if (error is ValidationError validation)
        {
            problem.Extensions[ErrorsExtension] = GroupByJsonKey(validation.Errors);
        }

        return problem;
    }

    /// <summary>
    /// W3C 추적 ID(32자리 16진수)를 돌려줍니다. 현재 <see cref="Activity"/>가 없거나 W3C 형식이 아니면
    /// <see cref="HttpContext.TraceIdentifier"/>를 씁니다(ADR-0024).
    /// </summary>
    private static string GetTraceId(HttpContext httpContext)
    {
        var activity = Activity.Current;
        return activity is not null && activity.IdFormat == ActivityIdFormat.W3C
            ? activity.TraceId.ToHexString()
            : httpContext.TraceIdentifier;
    }

    private static Dictionary<string, ProblemFieldError[]> GroupByJsonKey(IReadOnlyList<FieldError> errors)
    {
        // Dictionary는 삭제가 없으면 추가 순서대로 열거된다. 키 순서 = 처음 나온 순서.
        var grouped = new Dictionary<string, List<ProblemFieldError>>(StringComparer.Ordinal);
        foreach (var fieldError in errors)
        {
            var key = FieldErrorKeys.ToJsonKey(fieldError.PropertyName);
            if (!grouped.TryGetValue(key, out var items))
            {
                items = [];
                grouped.Add(key, items);
            }

            items.Add(new ProblemFieldError(fieldError.Code, fieldError.Message));
        }

        return grouped.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }
}
