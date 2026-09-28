using System.Text.Json;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EmergencyHub.Employee.Api.UnitTests;

// database.md "Api 등록 사양(S03-T04)", ADR-0011(연결 키 Write · Read), ADR-0019(Swagger Development만), logging-observability "헬스체크".
// 호스트를 시작하지 않고(연결 · 서버 없음) 등록과 미들웨어 파이프라인만 확인한다. 실제 DB 헬스 · HTTP 라우팅은 S03-T05 · T07.
// S05-T04에서 샘플 API를 지웠고, S06-T05에서 /api/employee Controller가 생겼다(HTTP 전 구간은 Acceptance/RegisterEmployeesHttpTests).
[Trait("FR", "PRD-001/FR-08")]
[Trait("FR", "PRD-001/FR-11")]
public sealed class ProgramTests : IDisposable
{
    private const string DummyWrite = "Host=localhost;Database=emergency_hub_employee;Username=employee_app";
    private const string DummyRead = "Host=localhost;Database=emergency_hub_employee;Username=employee_app;Options=-c default_transaction_read_only=on";
    private const string SwaggerPath = "/swagger/v1/swagger.json";

    // 테스트 출력 폴더에는 Api의 appsettings.json(파일 싱크)이 복사되므로 빈 폴더를 콘텐츠 루트로 쓴다.
    private readonly string _contentRoot = Directory.CreateTempSubdirectory("employee-api-tests-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    // ---- 성공 ----

    [Fact]
    public async Task ConfigureServices_WriteAndRead_BuildsWithScopeValidationAndResolvesSenderAndBothContexts()
    {
        // Development는 ValidateOnBuild · ValidateScopes가 켜진다(coding-conventions "DI 규칙").
        await using var app = BuildApp("Development", BothConnections());

        await using var scope = app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ISender>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Database.GetConnectionString().Should().Be(DummyWrite);
        scope.ServiceProvider.GetRequiredService<EmployeeReadDbContext>().Database.GetConnectionString().Should().Be(DummyRead, "읽기 연결 문자열을 고치거나 덧붙이지 않는다");
    }

    [Fact]
    public async Task ConfigureServices_HealthChecks_AreExactlyTwoDbContextChecksTaggedReady()
    {
        await using var app = BuildApp("Production", BothConnections());

        var registrations = app.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        registrations.Select(registration => registration.Name).Should().BeEquivalentTo(nameof(EmployeeDbContext), nameof(EmployeeReadDbContext));
        registrations.Should().OnlyContain(registration => registration.Tags.SetEquals(new[] { HealthEndpoints.ReadyTag }));
        registrations.Should().OnlyContain(registration => registration.FailureStatus == HealthStatus.Unhealthy, "failureStatus를 넘기지 않는다(기본값)");
    }

    [Fact]
    public void DbRetry_IsThreeRetriesWithFiveSecondMaxDelay()
    {
        // BL-073 확정값(S03-T06 실측, database.md "Api 등록 사양"): 3회 · 5초, Api 요청 제한 시간 없음.
        Program.DbRetry.MaxRetryCount.Should().Be(3);
        Program.DbRetry.MaxRetryDelay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ConfigurePipeline_Development_ServesOpenApiDocumentWithEmployeePathOnly()
    {
        // S06-T05: 일괄 등록 Controller(/api/employee POST)가 생겼다. 조회 2개는 S07에서 더한다(헬스 경로는 문서에 없음).
        await using var app = BuildApp("Development", BothConnections());
        Program.ConfigurePipeline(app);

        var context = await InvokeAsync(app, SwaggerPath);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        var document = ReadJson(context);
        document.GetProperty("info").GetProperty("title").GetString().Should().Be(Program.ApiTitle);
        document.GetProperty("paths").EnumerateObject().Select(path => path.Name).Should().Equal("/api/employee");
    }

    // ---- 실패 ----

    [Fact]
    public void ConfigureServices_WriteMissing_ThrowsWithoutConnectionValues()
    {
        var builder = CreateBuilder("Production", new Dictionary<string, string?> { ["ConnectionStrings:Read"] = "Host=db;Password=do-not-leak" });

        var act = () => Program.ConfigureServices(builder);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:Write*").Which.Message.Should().NotContain("do-not-leak");
    }

    [Fact]
    public void ConfigureServices_ReadMissing_ThrowsWithoutConnectionValues()
    {
        var builder = CreateBuilder("Production", new Dictionary<string, string?> { ["ConnectionStrings:Write"] = "Host=db;Password=do-not-leak" });

        var act = () => Program.ConfigureServices(builder);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:Read*").Which.Message.Should().NotContain("do-not-leak");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task ConfigurePipeline_NonDevelopment_DoesNotServeOpenApi(string environment)
    {
        await using var app = BuildApp(environment, BothConnections());
        Program.ConfigurePipeline(app);

        var context = await InvokeAsync(app, SwaggerPath);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task ConfigureServices_Development_DoesNotEnableSensitiveDataLogging()
    {
        // ADR-0020 opt-in 경로는 미구현(BL-094)이라 Development에서도 꺼져 있어야 한다. Program은 IsDevelopment로 판단하지 않는다.
        await using var app = BuildApp("Development", BothConnections());

        await using var scope = app.Services.CreateAsyncScope();
        OptionsOf<EmployeeDbContext>(scope.ServiceProvider).IsSensitiveDataLoggingEnabled.Should().BeFalse();
        OptionsOf<EmployeeReadDbContext>(scope.ServiceProvider).IsSensitiveDataLoggingEnabled.Should().BeFalse();
    }

    [Fact]
    public void ConfigureServices_WriteWhitespace_Throws()
    {
        var builder = CreateBuilder("Production", new Dictionary<string, string?> { ["ConnectionStrings:Write"] = "   ", ["ConnectionStrings:Read"] = DummyRead });

        var act = () => Program.ConfigureServices(builder);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConfigureServices_CalledTwice_Throws()
    {
        // AddConventionalServices · AddBuildingBlocksApi는 한 번만 부를 수 있다(두 겹 데코레이터 · 문서 중복 방지).
        var builder = CreateBuilder("Production", BothConnections());
        Program.ConfigureServices(builder);

        var act = () => Program.ConfigureServices(builder);

        act.Should().Throw<InvalidOperationException>();
    }

    private static Dictionary<string, string?> BothConnections() => new()
    {
        ["ConnectionStrings:Write"] = DummyWrite,
        ["ConnectionStrings:Read"] = DummyRead,
    };

    private static CoreOptionsExtension OptionsOf<TContext>(IServiceProvider services)
        where TContext : DbContext =>
        services.GetRequiredService<DbContextOptions<TContext>>().FindExtension<CoreOptionsExtension>()!;

    private static async Task<HttpContext> InvokeAsync(WebApplication app, string path)
    {
        var pipeline = ((IApplicationBuilder)app).Build();
        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        await pipeline(context);
        return context;
    }

    private static JsonElement ReadJson(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = JsonDocument.Parse(context.Response.Body);
        return document.RootElement.Clone();
    }

    private WebApplication BuildApp(string environment, IDictionary<string, string?> settings)
    {
        var builder = CreateBuilder(environment, settings);
        Program.ConfigureServices(builder);
        return builder.Build();
    }

    private WebApplicationBuilder CreateBuilder(string environment, IDictionary<string, string?> settings)
    {
        // ApplicationName = Api 어셈블리: MVC가 Controller를 이 어셈블리에서 찾는다(실행 시에는 진입 어셈블리).
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
            ContentRootPath = _contentRoot,
            ApplicationName = typeof(Program).Assembly.GetName().Name,
        });

        builder.Configuration.AddInMemoryCollection(settings);
        return builder;
    }
}
