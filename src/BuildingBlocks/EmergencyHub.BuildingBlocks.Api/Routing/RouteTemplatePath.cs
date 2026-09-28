using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EmergencyHub.BuildingBlocks.Api.Routing;

/// <summary>
/// 요청 경로 대신 기록 · 응답할 라우트 템플릿을 찾습니다(ADR-0025 "이름 경로 매개변수와 개인정보"). 경로 매개변수 값(이름 등 개인정보)이 남지 않습니다.
/// </summary>
/// <remarks>
/// <para>
/// 원본은 현재 엔드포인트, 없으면 <see cref="IExceptionHandlerFeature.Endpoint"/>의 <see cref="RouteEndpoint.RoutePattern"/> <c>RawText</c>입니다.
/// .NET 8 <c>ExceptionHandlerMiddleware</c>는 처리기를 부르기 전에 엔드포인트를 지우고 원래 값을 이 기능에 둡니다(500 경로).
/// </para>
/// <para>
/// <c>/</c>로 시작하지 않으면 붙이고 대소문자는 그대로 둡니다. <c>RouteEndpoint</c>가 아니거나(<c>[Consumes]</c> 불일치 415) 엔드포인트가 없거나
/// <c>RawText</c>가 없으면 <see langword="null"/>이고, 호출 쪽이 요청 경로를 씁니다.
/// ServiceDefaults에 같은 도우미가 있습니다(서로 참조할 수 없음, ADR-0024 의존성 표). 두 곳을 같은 테스트 표로 고정합니다.
/// </para>
/// </remarks>
internal static class RouteTemplatePath
{
    /// <summary>현재 요청의 라우트 템플릿을 돌려줍니다.</summary>
    /// <param name="httpContext">현재 요청.</param>
    /// <returns><c>/</c>로 시작하는 라우트 템플릿. 없으면 <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/>가 <see langword="null"/>인 경우.</exception>
    public static string? Find(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var endpoint = httpContext.GetEndpoint() ?? httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint;
        if (endpoint is not RouteEndpoint { RoutePattern.RawText: { } rawText })
        {
            return null;
        }

        return rawText.StartsWith('/') ? rawText : "/" + rawText;
    }
}
