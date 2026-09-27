using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Api.Logging;
using EmergencyHub.Employee.Infrastructure;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.ServiceDefaults;
using Serilog;

namespace EmergencyHub.Employee.Api;

/// <summary>
/// Employee HTTP API 진입점입니다(ADR-0016, ADR-0024, database.md "공통 DbContext 등록 > Api 등록 사양(S03-T04)").
/// </summary>
/// <remarks>
/// 최상위 문 대신 명시 클래스로 두어 sealed 규칙을 지키고, 통합 테스트(<c>WebApplicationFactory&lt;Program&gt;</c>, S03-T07)가
/// 형식 인자로 쓸 수 있게 public입니다. 마이그레이션은 적용하지 않습니다(MigrationService 1개가 적용, ADR-0012 · TD-011).
/// </remarks>
public sealed class Program
{
    /// <summary>OpenAPI 문서 제목입니다.</summary>
    internal const string ApiTitle = "Employee API";

    private Program()
    {
    }

    /// <summary>
    /// Api의 DB 재시도 설정입니다. BL-073 임시값, S03-T06 실측 뒤 확정합니다(재시도 총 대기 약 4.4초, 시도마다 연결 · 명령 제한 시간은 별도).
    /// </summary>
    internal static DbRetryOptions DbRetry { get; } = new(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5));

    /// <summary>호스트를 만들고 실행합니다.</summary>
    /// <param name="args">명령줄 인자.</param>
    /// <returns>호스트가 끝날 때 완료되는 작업.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder);

        await using var app = builder.Build();
        ConfigurePipeline(app);

        await app.RunAsync();
    }

    /// <summary>
    /// 서비스 등록: ServiceDefaults → Employee 등록 확장 하나(쓰기 · 읽기 DbContext, UnitOfWork, 규칙 기반 등록) → ready 헬스 검사 2개 → 공통 API 처리.
    /// </summary>
    /// <param name="builder">웹 애플리케이션 빌더.</param>
    /// <exception cref="InvalidOperationException">
    /// <c>ConnectionStrings:Write</c> · <c>ConnectionStrings:Read</c>가 없거나 비어 있는 경우, 또는 두 번 부른 경우.
    /// </exception>
    /// <remarks>
    /// 연결 문자열은 AppHost 환경 변수(<c>ConnectionStrings__Write</c> · <c>ConnectionStrings__Read</c>)로만 받습니다(설정 파일에 없음, ADR-0011).
    /// <c>EnableSensitiveDataLogging</c>은 여기서 판단 · 설정하지 않습니다(ADR-0020 opt-in 경로는 공용 등록 확장 한 곳, BL-094).
    /// 헬스 검사는 기본 검사(<c>CanConnectAsync</c>)라 읽기 전용 연결에서도 쓰기가 없습니다.
    /// </remarks>
    internal static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.Services.AddEmployeeInfrastructure(builder.Configuration, DbRetry);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<EmployeeDbContext>(tags: [HealthEndpoints.ReadyTag])
            .AddDbContextCheck<EmployeeReadDbContext>(tags: [HealthEndpoints.ReadyTag]);

        builder.Services.AddBuildingBlocksApi(ApiTitle);
    }

    /// <summary>
    /// 요청 파이프라인: 요청 로그(헬스 경로 제외) → 공통 API 처리(예외 처리 · Development 한정 Swagger · Controller) → 헬스 엔드포인트.
    /// </summary>
    /// <param name="app">웹 애플리케이션.</param>
    /// <remarks>요청 로그를 맨 앞에 두어 예외 처리기가 만든 최종 상태 코드(500 등)까지 한 줄로 남깁니다.</remarks>
    internal static void ConfigurePipeline(WebApplication app)
    {
        app.UseSerilogRequestLogging(options => options.GetLevel = (httpContext, _, exception) => RequestLogLevels.Get(httpContext, exception));
        app.UseBuildingBlocksApi();
        app.MapDefaultEndpoints();
    }
}
