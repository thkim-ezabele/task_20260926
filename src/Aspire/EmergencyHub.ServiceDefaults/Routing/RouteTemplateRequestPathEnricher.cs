using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace EmergencyHub.ServiceDefaults.Routing;

/// <summary>
/// 요청 안에서 남긴 로그의 <c>RequestPath</c> 속성을 라우트 템플릿으로 바꾸는 Serilog 보강기입니다(ADR-0025, NFR-04).
/// </summary>
/// <remarks>
/// <para>
/// ASP.NET Core 호스팅은 요청마다 로그 범위(<c>RequestId</c> · <c>RequestPath</c> · <c>ConnectionId</c>)를 열고, Serilog는 그 값을 요청 안의 모든
/// <c>ILogger&lt;T&gt;</c> 로그에 붙입니다. 범위의 <c>RequestPath</c>는 퍼센트 인코딩한 요청 경로라 <c>/api/employee/{name}</c>의 이름 값이 남습니다(S07-T02 실측).
/// </para>
/// <para>
/// 라우트 템플릿(<see cref="RouteTemplatePath"/>)이 있으면 <c>RequestPath</c>를 템플릿으로 덮어씁니다(범위 속성은 없을 때만 더해지므로 덮은 값이 남음).
/// 템플릿이 없으면(라우팅 전, 일치 없음) 그대로 둡니다. 요청 밖(Worker 등)에서는 아무것도 하지 않습니다.
/// </para>
/// </remarks>
/// <param name="httpContextAccessor">현재 요청 접근자.</param>
internal sealed class RouteTemplateRequestPathEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    /// <summary>호스팅 로그 범위 · 요청 완료 로그의 경로 속성 이름입니다.</summary>
    public const string RequestPathProperty = "RequestPath";

    /// <inheritdoc/>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        if (httpContextAccessor.HttpContext is { } httpContext && RouteTemplatePath.Find(httpContext) is { } template)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(RequestPathProperty, template));
        }
    }
}
