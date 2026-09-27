using Aspire.Hosting;

namespace EmergencyHub.AppHost.UnitTests;

// S04-T01 · BL-110: MigrationService에는 launchSettings가 없어 Aspire에서 Production으로 떴다.
// AppHost가 DOTNET_ENVIRONMENT=Development를 주입하고, Api는 기존대로 launchSettings(http 프로필)의 환경을 쓴다.
[Trait("FR", "PRD-001/FR-03")]
public sealed class ServiceEnvironmentTests
{
    private const string DotnetEnvironment = "DOTNET_ENVIRONMENT";
    private const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

    // ---- 성공 ----

    [Fact]
    public async Task Migrations_GetsDevelopmentEnvironment()
    {
        var (builder, resources) = AppHostModel.Create();

        var environment = await AppHostModel.RunEnvironmentAsync(builder, resources.EmployeeMigrations.Resource);

        environment.Should().Contain(DotnetEnvironment, "Development");
        EmergencyHubApplication.MigrationEnvironmentName.Should().Be("Development");
    }

    [Fact]
    public async Task Api_KeepsLaunchProfileEnvironmentWithoutInjectedDotnetEnvironment()
    {
        var (builder, resources) = AppHostModel.Create();

        var environment = await AppHostModel.RunEnvironmentAsync(builder, resources.EmployeeApi.Resource);

        environment.Should().Contain(AspNetCoreEnvironment, "Development");
        environment.Should().NotContainKey(DotnetEnvironment, "Api 환경은 launchSettings가 정하고 AppHost는 바꾸지 않는다");
    }

    // ---- 실패 ----

    [Fact]
    public async Task Postgres_DoesNotGetDotnetEnvironment()
    {
        var (_, resources) = AppHostModel.Create();

        var environment = await AppHostModel.EnvironmentExpressionsAsync(resources.Postgres.Resource);

        environment.Should().NotContainKey(DotnetEnvironment, "주입 대상은 MigrationService뿐이다");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Migrations_GetsDevelopmentEvenWhenAppHostRunsAsProduction()
    {
        var builder = AppHostModel.CreateBuilder(environmentName: "Production");
        var resources = builder.AddEmergencyHub(AppHostModel.PostgresImageTag);

        var environment = await AppHostModel.RunEnvironmentAsync(builder, resources.EmployeeMigrations.Resource);

        builder.Environment.EnvironmentName.Should().Be("Production");
        environment.Should().Contain(DotnetEnvironment, "Development");
    }
}
