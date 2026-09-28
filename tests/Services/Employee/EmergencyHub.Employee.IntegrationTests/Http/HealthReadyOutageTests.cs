using System.Diagnostics;
using System.Net;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.ServiceDefaults;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S03-T07(S03-T04 dba · S03-T06 tester 인계): DB가 멈춘 상태(docker pause)의 /health/ready 응답 시간 1회 측정.
// 실측(2026-09-28 로컬): 연결 풀 유무와 관계없이 약 15.0초 뒤 503 Unhealthy — Npgsql 연결 제한 시간(기본 15초)이고, 두 DbContext 검사는 병렬, 재시도 없음.
// 같은 컬렉션의 뒤 테스트를 지키려고 finally에서 반드시 재개한다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-03")]
public sealed class HealthReadyOutageTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);

    // ---- 실패 ----

    [Fact]
    public async Task GetReady_DatabasePaused_Returns503AfterConnectionTimeoutWhileLiveStays200ThenRecovers()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);
        using (var warm = await client.GetAsync(new Uri(HealthEndpoints.ReadyPath, UriKind.Relative), CancellationToken))
        {
            warm.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        TimeSpan elapsed;
        await Database.PauseContainerAsync(CancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            using var ready = await client.GetAsync(new Uri(HealthEndpoints.ReadyPath, UriKind.Relative), CancellationToken);
            elapsed = stopwatch.Elapsed;

            ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await ready.Content.ReadAsStringAsync(CancellationToken)).Should().Be("Unhealthy", "본문은 상태 문자열만");

            using var live = await client.GetAsync(new Uri(HealthEndpoints.LivePath, UriKind.Relative), CancellationToken);
            live.StatusCode.Should().Be(HttpStatusCode.OK, "live는 DB 검사를 하지 않는다");
        }
        finally
        {
            await Database.UnpauseContainerAsync();
        }

        TestContext.Current.TestOutputHelper?.WriteLine($"/health/ready (DB paused) elapsed: {elapsed.TotalMilliseconds:F0} ms");
        elapsed.Should().BeGreaterThanOrEqualTo(ConnectionTimeout - TimeSpan.FromSeconds(1), "응답은 연결 제한 시간까지 기다린다");
        elapsed.Should().BeLessThan(ConnectionTimeout * 2, "두 검사는 병렬이고 헬스 검사에는 재시도가 없다");

        using var recovered = await client.GetAsync(new Uri(HealthEndpoints.ReadyPath, UriKind.Relative), CancellationToken);
        recovered.StatusCode.Should().Be(HttpStatusCode.OK, "재개 뒤 같은 호스트가 바로 회복한다");
    }
}
