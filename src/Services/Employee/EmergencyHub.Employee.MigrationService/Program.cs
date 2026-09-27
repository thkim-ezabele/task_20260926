using EmergencyHub.Employee.Infrastructure;
using EmergencyHub.ServiceDefaults;

namespace EmergencyHub.Employee.MigrationService;

/// <summary>
/// Employee MigrationService 진입점입니다(ADR-0012). AppHost가 이 프로세스 하나만 실행하고(WithReplicas 없음) 종료 코드로 Api 시작을 정합니다.
/// </summary>
internal static class Program
{
    public static async Task Main(string[] args)
    {
        // 성공을 명시하기 전에는 실패로 둔다. Worker가 끝까지 돌지 못하고 호스트가 멈춰도 0으로 끝나지 않게 한다.
        Environment.ExitCode = (int)MigrationExitCode.Failed;

        var builder = Host.CreateApplicationBuilder(args);
        Configure(builder);

        using var host = builder.Build();
        await host.RunAsync();
    }

    /// <summary>
    /// 호스트 등록: ServiceDefaults + 쓰기 DbContext(Npgsql 기본 재시도, <c>ConnectionStrings:Write</c>) + Worker.
    /// 읽기 DbContext · UnitOfWork · 규칙 기반 등록은 하지 않습니다(database.md "MigrationService 동작 사양").
    /// </summary>
    /// <param name="builder">호스트 빌더.</param>
    /// <exception cref="InvalidOperationException">쓰기 연결 문자열이 없거나 비어 있는 경우(호스트 생성 전 종료).</exception>
    internal static void Configure(IHostApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.Services.AddEmployeeWriteDbContext(builder.Configuration);
        builder.Services.AddHostedService<MigrationWorker>();
    }
}
