using System.Net;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using EmergencyHub.ServiceDefaults;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S06-T01 완료 조건 ② · ④ 회귀(tester 보강): UseBuildingBlocksApi에 붙은 UseStatusCodePages는 415만 1005 ProblemDetails로 바꾸고,
// 실제 Employee Api 파이프라인(ServiceDefaults · 헬스 엔드포인트 포함)의 본문 없는 404 · 405는 그대로 둔다.
// S06-T05(tester T01 인계): 일괄 등록 엔드포인트의 [Consumes] 불일치 415 · 1005, 메서드 불일치 405(본문 없음),
// 폼 한도 초과 413 · 1004(TestServer는 Kestrel MaxRequestBodySize를 적용하지 않으므로 RequestFormLimits · RequestSizeLimit 메타데이터를 읽는 바인더 경로만 확인한다.
// Kestrel MaxRequestBodySize 413은 S06-T06 RegisterEmployeesKestrelLimitTests가 실제 Kestrel 호스트로 확인한다).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-09")]
public sealed class StatusCodePagesPipelineTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string RegisterPath = "/api/employee";

    // TestServer에서 [RequestSizeLimit]을 서버 한도로 적용하지 못하면 경고를 남기는 MVC 필터 범주입니다(Kestrel은 적용하므로 남기지 않음).
    internal const string RequestSizeLimitFilter = "Microsoft.AspNetCore.Mvc.Filters.RequestSizeLimitFilter";

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

    [Fact]
    public async Task Delete_RegisterEmployeesRoute_Returns405WithEmptyBody()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync(new Uri(RegisterPath, UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().BeEmpty("StatusCodePages 처리기는 415가 아니면 쓰지 않는다");
        response.Content.Headers.ContentType.Should().BeNull();
    }

    [Fact]
    public async Task Post_RegisterEmployeesWithUnsupportedContentType_Returns415With1005()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = new StringContent("김이름,kim@gmail.com,010-0000-0000,2000-01-01", System.Text.Encoding.UTF8, "text/plain");

        using var response = await client.PostAsync(new Uri(RegisterPath, UriKind.Relative), content, CancellationToken);

        await response.ShouldBeProblemAsync(
            HttpStatusCode.UnsupportedMediaType, 1005, CommonErrors.UnsupportedMediaType.Message, RegisterPath, CancellationToken);
    }

    [Fact]
    public async Task Post_RegisterEmployeesMultipartOverOneMebibyteOnTestServerRequestFormLimitsPathOnly_Returns413With1004AndStoresNothing()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = new MultipartFormDataContent { { new ByteArrayContent(new byte[(1024 * 1024) + 1]), "file", "employees.csv" } };

        using var response = await client.PostAsync(new Uri(RegisterPath, UriKind.Relative), content, CancellationToken);

        await response.ShouldBeProblemAsync(
            HttpStatusCode.RequestEntityTooLarge, 1004, CommonErrors.PayloadTooLarge.Message, RegisterPath, CancellationToken);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
        factory.Logs.Events.Should().Contain(
            logEvent => logEvent.SourceContext() == RequestSizeLimitFilter && logEvent.Level == Serilog.Events.LogEventLevel.Warning,
            "TestServer는 IHttpMaxRequestBodySizeFeature가 없어 [RequestSizeLimit]을 서버에 적용하지 못한다(413은 바인더가 메타데이터로 판정)");
    }

    // ---- 엣지: 지원하지 않는 Content-Type이어도 경로가 없으면 415가 아니라 본문 없는 404 ----
    // (헬스 엔드포인트는 MapHealthChecks라 모든 메서드를 받아 405 경로가 없다. 405는 일괄 등록 경로로 위에서 확인, S06-T05)

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
