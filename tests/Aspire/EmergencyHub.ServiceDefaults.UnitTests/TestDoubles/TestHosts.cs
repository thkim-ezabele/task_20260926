using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;

internal static class TestHosts
{
    // 설정 파일 · 사용자 비밀 · 환경 변수 없이 메모리 설정만 쓰는 Worker 호스트 빌더(MigrationService와 같은 IHostApplicationBuilder 경로).
    public static HostApplicationBuilder CreateWorkerBuilder(IDictionary<string, string?>? settings = null, string environment = "Testing")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            EnvironmentName = environment,
            ApplicationName = "EmergencyHub.ServiceDefaults.UnitTests",
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Configuration.AddInMemoryCollection(settings ?? new Dictionary<string, string?>());
        return builder;
    }

    // 루프백 Kestrel(포트 0)로 실제 HTTP 요청을 받는 웹 호스트 빌더.
    public static WebApplicationBuilder CreateWebBuilder(string environment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
            ApplicationName = "EmergencyHub.ServiceDefaults.UnitTests",
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");
        return builder;
    }

    public static Uri BaseAddressOf(WebApplication app) => new(app.Urls.First());
}
