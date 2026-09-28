using System.Net;
using EmergencyHub.Employee.IntegrationTests.Http;
using EmergencyHub.ServiceDefaults;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S06-T06 도구(developer): 임의 포트 실제 Kestrel 호스트(fixture DB 공유)의 동작 확인. 본 413 · 1004 입력 경로 테스트는 tester가 쓴다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-10")]
public sealed class EmployeeApiFactoryKestrelTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // ---- 성공 ----

    [Fact]
    public async Task CreateKestrelClient_UseKestrel_ServesReadyHealthOverLoopbackSocket()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { UseKestrel = true });
        using var client = factory.CreateKestrelClient();

        using var response = await client.GetAsync(new Uri(HealthEndpoints.ReadyPath, UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Kestrel 호스트도 fixture DB 연결을 쓴다");
        factory.KestrelAddress.Host.Should().Be("127.0.0.1");
        factory.KestrelAddress.Port.Should().BePositive("포트 0으로 열어 운영체제가 고른 포트를 받는다");
        factory.KestrelServices.GetRequiredService<IServer>().GetType().Name.Should().StartWith("KestrelServer");
        factory.Services.GetRequiredService<IServer>().Should().BeOfType<TestServer>("CreateClient는 계속 TestServer다");
    }

    [Fact]
    public async Task CreateKestrelClient_RequestLog_GoesToSharedCollector()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { UseKestrel = true });
        using var client = factory.CreateKestrelClient();
        factory.Logs.Clear();

        using var response = await client.DeleteAsync(new Uri("/api/employee", UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        factory.Logs.RequestStatusCodes("/api/employee").Should().Equal([405], "Kestrel 호스트의 요청 완료 로그도 같은 수집기로 온다(서버 쪽 상태 판정)");
    }

    // ---- 실패 ----

    [Fact]
    public async Task KestrelAddress_UseKestrelFalse_Throws()
    {
        await using var factory = new EmployeeApiFactory(Database);

        var act = () => factory.KestrelAddress;

        act.Should().Throw<InvalidOperationException>().WithMessage("*UseKestrel*");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task KestrelSend_ConnectionClosedByServer_ReturnsConnectionErrorInsteadOfThrowing()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { UseKestrel = true });
        using var client = factory.CreateKestrelClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/employee", UriKind.Relative))
        {
            Content = new ByteArrayContent(new byte[(4 * 1024 * 1024) + 1]) { Headers = { { "Content-Type", "text/csv" } } },
        };

        var outcome = await KestrelSend.SendAsync(client, request, CancellationToken);

        // 1 MiB를 크게 넘는 raw 본문은 서버가 413을 쓰고 연결을 닫아 클라이언트가 업로드 중 끊김을 받거나(HttpRequestException), 413 응답을 먼저 읽는다.
        (outcome.ConnectionError is not null || outcome.Response?.StatusCode == HttpStatusCode.RequestEntityTooLarge).Should().BeTrue();
        factory.Logs.RequestStatusCodes("/api/employee").Should().Equal([413], "서버 쪽 최종 상태는 요청 완료 로그로 판정한다");
        outcome.Response?.Dispose();
    }
}
