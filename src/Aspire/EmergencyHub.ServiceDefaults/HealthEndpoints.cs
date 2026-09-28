using Microsoft.AspNetCore.Http;

namespace EmergencyHub.ServiceDefaults;

/// <summary>
/// 헬스체크 경로와 태그입니다(logging-observability "헬스체크", BL-030).
/// </summary>
/// <remarks>
/// Aspire 템플릿의 <c>/health</c> · <c>/alive</c>(Development만) 대신 <c>/health/live</c> · <c>/health/ready</c>를 모든 환경에 매핑합니다.
/// 응답 본문은 상태 문자열(<c>Healthy</c> 등)뿐입니다.
/// </remarks>
public static class HealthEndpoints
{
    /// <summary>헬스 경로의 공통 접두사입니다. 추적 · 요청 로그 제외에 씁니다.</summary>
    public const string BasePath = "/health";

    /// <summary>프로세스 생존 확인 경로입니다. 등록된 검사를 하나도 실행하지 않습니다(DB 검사 없음).</summary>
    public const string LivePath = BasePath + "/live";

    /// <summary>의존성 준비 확인 경로입니다. <see cref="ReadyTag"/> 태그가 붙은 검사만 실행합니다.</summary>
    public const string ReadyPath = BasePath + "/ready";

    /// <summary>
    /// <see cref="ReadyPath"/>에 포함할 검사의 태그입니다. 서비스 Api가 DB 검사(<c>AddDbContextCheck</c>)를 이 태그로 등록합니다.
    /// </summary>
    public const string ReadyTag = "ready";

    /// <summary>요청 경로가 헬스 경로(<see cref="BasePath"/> 아래)인지 판별합니다.</summary>
    /// <param name="path">요청 경로.</param>
    /// <returns>헬스 경로이면 <see langword="true"/>.</returns>
    public static bool IsHealthPath(PathString path) => path.StartsWithSegments(BasePath, StringComparison.OrdinalIgnoreCase);
}
