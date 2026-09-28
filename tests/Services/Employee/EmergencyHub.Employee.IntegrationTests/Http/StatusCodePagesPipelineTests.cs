using System.Net;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.ServiceDefaults;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S06-T01 완료 조건 ② · ④ 회귀(tester 보강): UseBuildingBlocksApi에 붙은 UseStatusCodePages는 415만 1005 ProblemDetails로 바꾸고,
// 실제 Employee Api 파이프라인(ServiceDefaults · 헬스 엔드포인트 포함)의 본문 없는 404 · 405는 그대로 둔다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-09")]
public sealed class StatusCodePagesPipelineTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // ---- 성공: 기존 경로는 그대로 ----

    [Fact]
    public async Task GetLive_ExistingEndpoint_Returns200Unchanged()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(HealthEndpoints.LivePath, UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().NotBe(HttpProblem.ContentType);
    }

    // ---- 실패: 415가 아닌 본문 없는 응답은 바꾸지 않음 ----

    [Fact]
    public async Task Get_UnknownRoute_Returns404WithEmptyBody()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/unknown-route", UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().BeEmpty("StatusCodePages 처리기는 415가 아니면 쓰지 않는다");
        response.Content.Headers.ContentType.Should().BeNull();
    }

    // ---- 엣지: 지원하지 않는 Content-Type이어도 경로가 없으면 415가 아니라 본문 없는 404 ----
    // (헬스 엔드포인트는 MapHealthChecks라 모든 메서드를 받아 405 경로가 없다. Controller가 생기는 S06-T05 이후 405는 그 작업에서 확인)

    [Fact]
    public async Task Post_UnknownRouteWithUnsupportedContentType_Returns404WithEmptyBody()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = new StringContent("hong@example.com", System.Text.Encoding.UTF8, "text/plain");

        using var response = await client.PostAsync(new Uri("/api/v1/unknown-route", UriKind.Relative), content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().BeEmpty("StatusCodePages 처리기는 415가 아니면 쓰지 않는다");
        response.Content.Headers.ContentType.Should().BeNull();
    }
}
