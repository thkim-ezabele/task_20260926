using System.Net;
using EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmergencyHub.ServiceDefaults.UnitTests;

// S03-T03 · BL-030: /health/live(검사 없음) · /health/ready('ready' 태그 검사만)를 모든 환경에 매핑하고, 본문은 상태 문자열만.
// 루프백 Kestrel에 실제 요청을 보낸다.
[Trait("FR", "PRD-001/FR-03")]
public sealed class HealthEndpointsTests
{
    private const string SecretDetail = "Host=db;Password=do-not-leak";

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task MapDefaultEndpoints_AnyEnvironment_MapsLiveAndReadyWithStatusOnlyBody(string environment)
    {
        await using var app = await StartAsync(environment);
        using var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) };

        var live = await GetAsync(client, HealthEndpoints.LivePath);
        var ready = await GetAsync(client, HealthEndpoints.ReadyPath);

        live.Should().Be((HttpStatusCode.OK, "Healthy"));
        ready.Should().Be((HttpStatusCode.OK, "Healthy"));
    }

    [Fact]
    public async Task MapDefaultEndpoints_ReadyTaggedCheckUnhealthy_ReadyReturns503ButLiveStaysHealthy()
    {
        await using var app = await StartAsync("Production", checks => checks.AddCheck(
            "db",
            () => HealthCheckResult.Unhealthy(SecretDetail, new InvalidOperationException(SecretDetail)),
            [HealthEndpoints.ReadyTag]));
        using var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) };

        var ready = await GetAsync(client, HealthEndpoints.ReadyPath);
        var live = await GetAsync(client, HealthEndpoints.LivePath);

        // 본문은 상태만: 검사 이름 · 설명 · 예외(연결 정보 등)가 나가지 않는다.
        ready.Should().Be((HttpStatusCode.ServiceUnavailable, "Unhealthy"));
        live.Should().Be((HttpStatusCode.OK, "Healthy"));
    }

    [Fact]
    public async Task MapDefaultEndpoints_UntaggedUnhealthyCheck_IsExcludedFromReadyAndLive()
    {
        await using var app = await StartAsync("Production", checks => checks.AddCheck("untagged", () => HealthCheckResult.Unhealthy()));
        using var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) };

        (await GetAsync(client, HealthEndpoints.ReadyPath)).Should().Be((HttpStatusCode.OK, "Healthy"));
        (await GetAsync(client, HealthEndpoints.LivePath)).Should().Be((HttpStatusCode.OK, "Healthy"));
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task MapDefaultEndpoints_AspireTemplatePaths_AreNotMapped(string path)
    {
        await using var app = await StartAsync("Development");
        using var client = new HttpClient { BaseAddress = TestHosts.BaseAddressOf(app) };

        (await GetAsync(client, path)).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void MapDefaultEndpoints_NullApp_Throws()
    {
        var act = () => ServiceDefaultsExtensions.MapDefaultEndpoints(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("/health", true)]
    [InlineData("/health/live", true)]
    [InlineData("/HEALTH/READY", true)]
    [InlineData("/healthz", false)]
    [InlineData("/api/v1/employees", false)]
    [InlineData("", false)]
    public void IsHealthPath_Path_ReturnsWhetherUnderHealthSegment(string path, bool expected)
    {
        HealthEndpoints.IsHealthPath(new PathString(path)).Should().Be(expected);
    }

    private static async Task<WebApplication> StartAsync(string environment, Action<IHealthChecksBuilder>? checks = null)
    {
        var builder = TestHosts.CreateWebBuilder(environment);
        builder.AddServiceDefaults();
        checks?.Invoke(builder.Services.AddHealthChecks());

        var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static async Task<(HttpStatusCode Status, string Body)> GetAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return (response.StatusCode, body);
    }
}
