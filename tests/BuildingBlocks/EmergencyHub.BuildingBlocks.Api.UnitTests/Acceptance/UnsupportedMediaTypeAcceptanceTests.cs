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

// S06-T01 완료 조건 ②(ADR-0028 "[Consumes] 불일치 415"): 표본 Controller를 TestServer로 실행해 Controller · 필터 전 구간을 지난다.
// 실측 경로 2개: (1) [Consumes]에 없는 Content-Type → 엔드포인트 라우팅(ConsumesMatcherPolicy)이 본문 없는 415로 끝냄 → UseStatusCodePages 처리기
// (2) [FromBody] 액션에 Content-Type 없음 → UnsupportedContentTypeFilter → UnsupportedMediaTypeResult → ClientErrorResultFilter → IClientErrorFactory 데코레이터.
// 둘 다 공통 ProblemDetails(415 · 1005, code · traceId)이고, 그 밖의 클라이언트 오류 결과(404 NotFound() 등)는 프레임워크 기본 그대로다.
[Trait("FR", "PRD-002/FR-09")]
public sealed class UnsupportedMediaTypeAcceptanceTests : IAsyncLifetime
{
    private const string ImportPath = "/" + ConsumesSamplesController.RoutePath;
    private const string JsonBodyPath = ImportPath + "/" + ConsumesSamplesController.JsonBodyPath;
    private const string MissingPath = ImportPath + "/" + ConsumesSamplesController.MissingPath;

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddBuildingBlocksApi("Sample API");

        _app = builder.Build();
        _app.UseBuildingBlocksApi();
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    // ---- 성공 ----

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("image/png")]
    public async Task Post_UnsupportedContentType_Responds415With1005ProblemDetails(string contentType)
    {
        using var response = await PostAsync(ImportPath, "hong@example.com", contentType);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        var json = await ReadProblemAsync(response);
        json.GetProperty("type").GetString().Should().Be("https://httpstatuses.io/415");
        json.GetProperty("title").GetString().Should().Be("Unsupported Media Type");
        json.GetProperty("status").GetInt32().Should().Be(415);
        json.GetProperty("detail").GetString().Should().Be(CommonErrors.UnsupportedMediaType.Message);
        // ADR-0025 fallback: [Consumes] 불일치는 라우팅이 RouteEndpoint가 아닌 415 엔드포인트를 골라 라우트 템플릿이 없다. instance는 요청 경로 그대로다.
        // 경로 매개변수가 있는 라우트에서도 요청 경로인지는 RouteTemplateAcceptanceTests가 본다.
        json.GetProperty("instance").GetString().Should().Be(ImportPath);
        json.GetProperty("code").GetInt32().Should().Be(1005);
        json.TryGetProperty("errors", out _).Should().BeFalse();
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().NotContain("hong@example.com");
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/csv")]
    [InlineData("application/x-www-form-urlencoded")]
    [InlineData("application/json; charset=utf-8")]
    public async Task Post_SupportedContentType_ReachesAction(string contentType)
    {
        using var response = await PostAsync(ImportPath, "{}", contentType);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PostFromBody_UnsupportedContentType_Responds415With1005()
    {
        using var response = await PostAsync(JsonBodyPath, "a,b", "text/csv");

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        (await ReadProblemAsync(response)).GetProperty("code").GetInt32().Should().Be(1005);
    }

    // ---- 실패: 415가 아닌 클라이언트 오류 결과는 바꾸지 않음 ----

    [Fact]
    public async Task NotFoundResult_OtherClientError_KeepsFrameworkProblemDetailsWithoutCode()
    {
        using var response = await _client.GetAsync(new Uri(MissingPath, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        document.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        document.RootElement.TryGetProperty("code", out _).Should().BeFalse();
    }

    // ---- 엣지: Content-Type 없음 ----

    [Fact]
    public async Task Post_NoContentTypeWithBody_OnFromBodyAction_Responds415With1005()
    {
        // 실측: Content-Type이 없으면 입력 포맷터를 고를 수 없어 UnsupportedContentTypeFilter가 415(UnsupportedMediaTypeResult)를 만든다.
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"));
        using var response = await _client.PostAsync(new Uri(JsonBodyPath, UriKind.Relative), content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        (await ReadProblemAsync(response)).GetProperty("code").GetInt32().Should().Be(1005);
    }

    [Fact]
    public async Task Post_NoContentTypeWithBody_OnActionWithoutFromBody_ReachesAction()
    {
        // 실측: [FromBody]가 없는 액션은 Content-Type이 없으면 [Consumes] 불일치로 보지 않고(ConsumesAttribute · ConsumesMatcherPolicy 기본 동작)
        // 액션까지 간다. 일괄 등록처럼 전용 바인더가 본문을 읽는 액션은 바인더가 판정한다(ADR-0026 1절 형식 판별, S06-T05 인계).
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes("hong@example.com"));
        using var response = await _client.PostAsync(new Uri(ImportPath, UriKind.Relative), content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<HttpResponseMessage> PostAsync(string path, string body, string contentType)
    {
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        return await _client.PostAsync(new Uri(path, UriKind.Relative), content, TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be(ErrorProblemDetails.ContentType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var json = document.RootElement.Clone();
        json.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number, "ADR-0008: 에러 코드는 정수");
        json.GetProperty("traceId").ValueKind.Should().Be(JsonValueKind.String);
        return json;
    }
}
