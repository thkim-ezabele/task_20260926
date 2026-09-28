using System.Net;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.FaultInjection;
using EmergencyHub.Employee.IntegrationTests.Http;
using EmergencyHub.Employee.IntegrationTests.TestData;
using EmergencyHub.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T07 도우미 스모크(완료 조건 "WebApplicationFactory 호스트는 T06 fixture를 공유하고, 쓰기 DbContext 재등록 · TimeProvider 교체 · 로그 수집 sink 주입 도우미를 쓴다").
// HTTP 인수 시나리오(등록 → 조회, 409, 1001 · 1002 · 21006 · 9001, traceId 등)는 tester가 작성한다.
// S05-T04: PRD-001 샘플 API를 지워 Controller가 없다. TimeProvider · 인터셉터 도우미는 Api 호스트 DI의 Repository · UnitOfWork 커밋으로 확인한다
// (HTTP 등록 경로는 S06-T06). 새 스키마(S05-T05) 전에는 옛 테이블과 컬럼이 달라 커밋이 실패한다(실패 허용 목록).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-09")]
public sealed class EmployeeApiFactoryTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
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

    // S07-T03 도구(developer): 호스트는 Api 실제 appsettings.json의 Microsoft.AspNetCore Warning 재정의를 그대로 쓴다(완료 조건 ④의 전제).
    // 호스팅 'Request starting' · 'Request finished'(Information)는 경로 원문(이름 값)을 담으므로 수집되면 안 된다.
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Logs_ApiSettingsOverride_AspNetCoreInformationLogsNotCollected(string environment)
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { Environment = environment });
        using var client = factory.CreateClient();
        factory.Logs.Clear();

        using var response = await client.GetAsync(new Uri("/api/employee/" + Uri.EscapeDataString(EmployeeBulkSeeder.MissingName), UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        factory.Services.GetRequiredService<IConfiguration>()["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"].Should().Be("Warning");
        factory.Logs.RequestCompletions.Should().ContainSingle("수집 자체는 동작한다(빈 수집으로 통과하지 않음)");
        factory.Logs.Events.Where(logEvent => logEvent.SourceContext().StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .Should().AllSatisfy(logEvent => logEvent.Level.Should().BeOneOf(Serilog.Events.LogEventLevel.Warning, Serilog.Events.LogEventLevel.Error, Serilog.Events.LogEventLevel.Fatal));
    }

    [Fact]
    public async Task TimeProvider_Replaced_AuditTimestampsUseFakeTime()
    {
        var now = new DateTimeOffset(2026, 9, 28, 1, 2, 3, TimeSpan.Zero);
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { TimeProvider = new FakeTimeProvider(now) });
        var employee = new EmployeeBuilder().Build();

        (await EmployeeCommits.AddAndCommitAsync(factory.Services, employee, CancellationToken)).IsSuccess.Should().BeTrue();

        await using var scope = factory.Services.CreateAsyncScope();
        var audit = await scope.ServiceProvider.GetRequiredService<EmployeeReadDbContext>().Set<Domain.Employees.Employee>()
            .Where(stored => stored.Id == employee.Id)
            .Select(stored => new
            {
                CreatedAt = EF.Property<DateTimeOffset>(stored, ShadowPropertyNames.CreatedAt),
                UpdatedAt = EF.Property<DateTimeOffset>(stored, ShadowPropertyNames.UpdatedAt),
            })
            .SingleAsync(CancellationToken);
        audit.CreatedAt.Should().Be(now);
        audit.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public async Task WriteInterceptors_Registered_ObserveApiHostUnitOfWorkCommit()
    {
        var probe = new TransactionProbeInterceptor();
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteInterceptors = [probe] });

        (await EmployeeCommits.AddAndCommitAsync(factory.Services, new EmployeeBuilder().Build(), CancellationToken)).IsSuccess.Should().BeTrue();

        probe.Commits.Should().Be(1, "운영 옵션에 덧붙인 인터셉터가 Api 호스트 UnitOfWork 트랜잭션을 본다");
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
}
