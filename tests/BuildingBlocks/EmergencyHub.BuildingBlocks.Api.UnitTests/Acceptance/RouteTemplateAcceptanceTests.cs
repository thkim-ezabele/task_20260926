using System.Net;
using System.Text;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Acceptance;

// S07-T02(ADR-0025 "이름 경로 매개변수와 개인정보"): 공통 API 처리(UseBuildingBlocksApi)를 TestServer로 실행해 라우팅 · MVC · 예외 처리 전 구간에서
// ProblemDetails.instance가 라우트 템플릿(/api/v1/route-samples/{name})이고 경로 매개변수 값(홍길동)이 응답에 없는지 본다.
// 템플릿이 없는 응답([Consumes] 불일치 415, 일치 없음)은 요청 경로 그대로다.
[Trait("FR", "PRD-002/FR-08")]
[Trait("NFR", "PRD-002/NFR-04")]
public sealed class RouteTemplateAcceptanceTests : IAsyncLifetime
{
    private const string Name = "홍길동";
    private const string EncodedName = "%ED%99%8D%EA%B8%B8%EB%8F%99";
    private const string BasePath = "/" + RouteTemplateSamplesController.RoutePath;

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddBuildingBlocksApi("Sample API");

        _app = builder.Build();
        _app.UseBuildingBlocksApi();
        await _app.StartAsync(CancellationToken);
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    // ---- 성공: 템플릿 있음 ----

    [Fact]
    public async Task FailureResult_OnNameRoute_InstanceIsTemplate()
    {
        using var response = await _client.GetAsync($"{BasePath}/{EncodedName}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var (json, body) = await ReadProblemAsync(response);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.NotFound.Code);
        json.GetProperty("instance").GetString().Should().Be(RouteTemplateSamplesController.Template);
        AssertNoName(body);
    }

    [Fact]
    public async Task BindingError_OnNameRoute_InstanceIsTemplate()
    {
        using var response = await _client.GetAsync($"{BasePath}/{EncodedName}/count?count=abc", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (json, body) = await ReadProblemAsync(response);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.ValidationFailed.Code);
        json.GetProperty("instance").GetString().Should().Be(RouteTemplateSamplesController.Template + "/count");
        AssertNoName(body);
    }

    [Fact]
    public async Task UnsupportedMediaTypeFromFilter_OnNameRoute_InstanceIsTemplate()
    {
        // [FromBody] 액션에 Content-Type 없음 → 액션 필터의 415(RouteEndpoint 선택 뒤)라 템플릿이 있다.
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"));
        using var response = await _client.PostAsync(new Uri($"{BasePath}/{EncodedName}/json", UriKind.Relative), content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        var (json, body) = await ReadProblemAsync(response);
        json.GetProperty("instance").GetString().Should().Be(RouteTemplateSamplesController.Template + "/json");
        AssertNoName(body);
    }

    // ---- 실패(500 경로): 예외 처리기가 엔드포인트를 지운 뒤에도 템플릿 ----

    [Fact]
    public async Task UnhandledException_OnNameRoute_InstanceIsTemplateFromExceptionHandlerFeature()
    {
        using var response = await _client.GetAsync($"{BasePath}/{EncodedName}/throw", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var (json, body) = await ReadProblemAsync(response);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.Unexpected.Code);
        json.GetProperty("instance").GetString().Should().Be(RouteTemplateSamplesController.Template + "/throw");
        AssertNoName(body);
    }

    // ---- 엣지: 템플릿 없음 → 요청 경로 유지 ----

    [Fact]
    public async Task ConsumesMismatch_OnNameRoute_InstanceKeepsRequestPath()
    {
        // ADR-0025 fallback: [Consumes] 불일치 415는 RouteEndpoint가 아닌 엔드포인트라 템플릿이 없다. {name} 조회(GET)에는 [Consumes]가 없다.
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes("a"));
        content.Headers.TryAddWithoutValidation("Content-Type", "text/plain");
        using var response = await _client.PostAsync(new Uri($"{BasePath}/abc", UriKind.Relative), content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        var (json, _) = await ReadProblemAsync(response);
        json.GetProperty("code").GetInt32().Should().Be(CommonErrors.UnsupportedMediaType.Code);
        json.GetProperty("instance").GetString().Should().Be($"{BasePath}/abc");
    }

    private static void AssertNoName(string body)
    {
        body.Should().NotContain(Name).And.NotContain(EncodedName).And.NotContain("\uD64D", "JSON 이스케이프한 이름(홍)도 없다");
    }

    private static async Task<(JsonElement Json, string Body)> ReadProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be(ErrorProblemDetails.ContentType);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        using var document = JsonDocument.Parse(body);
        return (document.RootElement.Clone(), body);
    }
}
