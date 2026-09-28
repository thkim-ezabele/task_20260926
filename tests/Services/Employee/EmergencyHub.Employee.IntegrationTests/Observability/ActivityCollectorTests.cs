using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Observability;

// S06-T06 도구(developer): Npgsql span 수집기. BL-024 본 테스트(Api 요청의 span 태그에 비밀번호 · 입력 값 없음)는 tester가 쓴다.
// S07-T03 도구(developer): ASP.NET Core 요청 span 수집(소스 지정 · trace ID / server.port로 고르기 · 끝날 때까지 대기).
// {name} 라우트의 200 · 400 · 404 · 500 url.path 이름 부재 본 테스트(완료 조건 ④)는 tester가 쓴다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/NFR-04")]
public sealed class ActivityCollectorTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string Probe = "SELECT 'npgsql-activity-probe'";
    private const string NameTemplatePath = "/api/employee/{name}";
    private const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

    // ---- 성공 ----

    [Fact]
    public async Task Activities_CommandExecuted_CapturesSpanWithStatementTag()
    {
        using var collector = new ActivityCollector(ActivityCollector.NpgsqlSource);

        await ExecuteProbeAsync();

        var span = collector.Activities.Should().ContainSingle(activity => activity.Tag("db.statement") == Probe).Which;
        span.SourceName.Should().Be(ActivityCollector.NpgsqlSource);
        span.Tags.Select(tag => tag.Key).Should().Contain("db.system");
        span.Dump().Should().Contain(Probe);
    }

    [Fact]
    public async Task WaitForAsync_TestServerRequestWithTraceparent_ReturnsRequestSpanByTraceIdWithTemplateUrlPath()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var collector = new ActivityCollector(ActivityCollector.AspNetCoreSource);
        using var request = new HttpRequestMessage(HttpMethod.Get, NamePath(EmployeeBulkSeeder.MissingName));
        request.Headers.Add("traceparent", $"00-{TraceId}-00f067aa0ba902b7-01");

        using var response = await client.SendAsync(request, CancellationToken);

        var span = (await collector.WaitForAsync(activity => activity.TraceId == TraceId, CancellationToken)).Should().ContainSingle(
            "TestServer 클라이언트는 HttpClient 계측이 없어 보낸 traceparent의 trace ID를 서버 span이 잇는다").Which;
        span.SourceName.Should().Be(ActivityCollector.AspNetCoreSource);
        span.Tag("http.response.status_code").Should().Be("404");
        span.Tag("url.path").Should().Be(NameTemplatePath, "호스트의 ServiceDefaults 계측(EnrichWithHttpResponse)이 exporter 없이도 동작한다");
    }

    // ---- 실패 ----

    [Fact]
    public async Task Activities_AfterDispose_CollectsNothingMore()
    {
        var collector = new ActivityCollector(ActivityCollector.NpgsqlSource);
        collector.Dispose();

        await ExecuteProbeAsync();

        collector.Activities.Should().BeEmpty();
    }

    [Fact]
    public async Task WaitForAsync_NoMatchWithinTimeout_ReturnsEmptyAndInvalidCountThrows()
    {
        using var collector = new ActivityCollector(ActivityCollector.AspNetCoreSource);

        var matched = await collector.WaitForAsync(_ => true, CancellationToken, timeout: TimeSpan.FromMilliseconds(100));
        var invalid = () => collector.WaitForAsync(_ => true, CancellationToken, count: 0);

        matched.Should().BeEmpty("시간 안에 모이지 않으면 예외 없이 그때까지 맞은 span만 돌려준다(개수 단언은 호출자)");
        await invalid.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Activities_FilterRejects_SkipsSpanButClearResetsList()
    {
        using var all = new ActivityCollector(ActivityCollector.NpgsqlSource);
        using var none = new ActivityCollector(ActivityCollector.NpgsqlSource, _ => false);

        await ExecuteProbeAsync();

        none.Activities.Should().BeEmpty("필터가 거부한 span은 모으지 않는다");
        all.Activities.Should().NotBeEmpty("리스너 여러 개가 같은 span을 각자 받는다");
        all.Clear();
        all.Activities.Should().BeEmpty();
    }

    [Fact]
    public async Task ServerPort_KestrelRequest_SelectsServerSpanEvenThoughHttpClientReplacesTraceparent()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { UseKestrel = true });
        using var client = factory.CreateKestrelClient();
        using var collector = new ActivityCollector(ActivityCollector.AspNetCoreSource);
        using var request = new HttpRequestMessage(HttpMethod.Get, NamePath(EmployeeBulkSeeder.MissingName));
        request.Headers.Add("traceparent", $"00-{TraceId}-00f067aa0ba902b7-01");

        using var response = await client.SendAsync(request, CancellationToken);

        var span = (await collector.WaitForAsync(ActivityCollector.ServerPort(factory.KestrelAddress.Port), CancellationToken)).Should().ContainSingle().Which;
        span.Tag("url.path").Should().Be(NameTemplatePath);
        span.Tag("http.response.status_code").Should().Be("404");
    }

    private static Uri NamePath(string name) => new("/api/employee/" + Uri.EscapeDataString(name), UriKind.Relative);

    private async Task ExecuteProbeAsync()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await using var command = new NpgsqlCommand(Probe, connection);
        await command.ExecuteScalarAsync(CancellationToken);
    }
}
