using System.Net;
using System.Net.Http.Json;
using EmergencyHub.Employee.IntegrationTests.FaultInjection;
using EmergencyHub.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T07 도우미 스모크(완료 조건 "WebApplicationFactory 호스트는 T06 fixture를 공유하고, 쓰기 DbContext 재등록 · TimeProvider 교체 · 로그 수집 sink 주입 도우미를 쓴다").
// HTTP 인수 시나리오(등록 → 조회, 409, 1001 · 1002 · 21006 · 9001, traceId 등)는 tester가 작성한다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-09")]
public sealed class EmployeeApiFactoryTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string EmployeesPath = "/api/v1/employees";

    // ---- 성공 ----

    [Fact]
    public async Task CreateClient_FixtureConnections_ReadyHealthReturns200Healthy()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(HealthEndpoints.ReadyPath, UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().Be("Healthy");
    }

    [Fact]
    public async Task Logs_HostLogger_CollectsEventWithServiceNameFromSettings()
    {
        await using var factory = new EmployeeApiFactory(Database);
        factory.Logs.Clear();

        factory.Services.GetRequiredService<ILogger<EmployeeApiFactoryTests>>().FactorySmoke(7);

        // Serilog:Properties(ServiceName)는 설정 사본에 남아 있어 수집 이벤트에 붙는다.
        var logEvent = factory.Logs.Events.Should().ContainSingle(e => e.MessageTemplate.Text == FactorySmokeLogs.Template).Which;
        logEvent.Properties["Value"].ToString().Should().Be("7");
        logEvent.Properties["ServiceName"].ToString().Should().Be("\"employee\"");
    }

    [Fact]
    public async Task TimeProvider_Replaced_AuditTimestampsUseFakeTime()
    {
        var now = new DateTimeOffset(2026, 9, 28, 1, 2, 3, TimeSpan.Zero);
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { TimeProvider = new FakeTimeProvider(now) });
        using var client = factory.CreateClient();

        var id = await RegisterAsync(client, "time@example.com");
        using var get = await client.GetAsync(new Uri($"{EmployeesPath}/{id}", UriKind.Relative), CancellationToken);

        var body = await get.Content.ReadFromJsonAsync<EmployeeBody>(CancellationToken);
        body!.CreatedAt.Should().Be(now);
        body.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public async Task WriteInterceptors_Registered_ObserveApiCommandCommit()
    {
        var probe = new TransactionProbeInterceptor();
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteInterceptors = [probe] });
        using var client = factory.CreateClient();

        await RegisterAsync(client, "probe@example.com");

        probe.Commits.Should().Be(1, "운영 옵션에 덧붙인 인터셉터가 Api Command 트랜잭션을 본다");
    }

    // ---- 실패 ----

    [Fact]
    public async Task CreateClient_WrongWritePassword_ReadyHealthReturns503()
    {
        var wrong = Database.WriteConnectionStringWith(builder => builder.Password = "wrong-password");
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteConnectionString = wrong });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(HealthEndpoints.ReadyPath, UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "주입한 연결 문자열이 운영 등록에 그대로 쓰인다");
    }

    [Fact]
    public void Constructor_NullDatabase_Throws()
    {
        var act = () => new EmployeeApiFactory(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Configuration_ContentRootCopy_HasNoSerilogSinksButKeepsLevelsAndNoOtlpEndpoint()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath.TrimEnd(Path.DirectorySeparatorChar)
            .Should().Be(factory.ContentRoot.TrimEnd(Path.DirectorySeparatorChar));
        configuration.GetSection("Serilog:WriteTo").Exists().Should().BeFalse("콘솔 · 파일 싱크를 쓰지 않는다");
        configuration["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"].Should().Be("Warning");
        configuration["Serilog:MinimumLevel:Default"].Should().Be("Debug", "Development 설정 파일도 복사된다");
        OtlpEndpoint.IsConfigured(configuration).Should().BeFalse();
    }

    [Fact]
    public async Task Dispose_Factory_DeletesContentRootCopy()
    {
        var factory = new EmployeeApiFactory(Database);
        var contentRoot = factory.ContentRoot;
        Directory.Exists(contentRoot).Should().BeTrue();

        await factory.DisposeAsync();

        Directory.Exists(contentRoot).Should().BeFalse();
    }

    private async Task<Guid> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            EmployeesPath,
            new { displayName = "Factory Smoke", email, employeeStatus = 1 },
            CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedBody>(CancellationToken))!.Id;
    }

    private sealed record CreatedBody(Guid Id);

    private sealed record EmployeeBody(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
}
