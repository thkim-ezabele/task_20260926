using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using EmergencyHub.ServiceDefaults;

namespace EmergencyHub.AppHost.UnitTests;

// S03-T05 · ADR-0011 · database.md "로컬 DB 구성 (AppHost)": WithReference 없이 조립한 Write / Read 연결(employee_app, Read에만 read_only),
// MigrationService는 Write만 · WaitFor(db), Api는 Write · Read · WaitFor(db) · WaitForCompletion(migrations) · /health/ready HTTP 헬스.
[Trait("FR", "PRD-001/FR-03")]
[Trait("FR", "PRD-001/FR-08")]
[Trait("NFR", "PRD-001/NFR-06")]
public sealed class ServiceResourceTests
{
    private const string WriteKey = "ConnectionStrings__Write";
    private const string ReadKey = "ConnectionStrings__Read";
    private const string ServerPrefix =
        "Host={postgres.bindings.tcp.host};Port={postgres.bindings.tcp.port};Database=emergency_hub_employee;Username=employee_app;Password={employee-app-password.value}";

    // ---- 성공 ----

    [Fact]
    public void AddEmergencyHub_RegistersResourcesWithFixedNames()
    {
        var (builder, resources) = AppHostModel.Create();

        builder.Resources.Select(resource => resource.Name).Should().BeEquivalentTo(
            "postgres-password", "employee-app-password", "postgres", "employee-db", "employee-migrations", "employee-api");
        (resources.EmployeeMigrations.Resource.Name, resources.EmployeeApi.Resource.Name).Should().Be(("employee-migrations", "employee-api"));
    }

    [Fact]
    public async Task Migrations_GetsOnlyWriteConnectionAsEmployeeApp()
    {
        var (_, resources) = AppHostModel.Create();

        var environment = await AppHostModel.EnvironmentExpressionsAsync(resources.EmployeeMigrations.Resource);

        ConnectionStrings(environment).Should().BeEquivalentTo(new Dictionary<string, string>
        {
            [WriteKey] = $"{ServerPrefix};Application Name=employee-migration",
        });
    }

    [Fact]
    public async Task Api_GetsWriteAndReadOnlyReadConnectionsAsEmployeeApp()
    {
        var (_, resources) = AppHostModel.Create();

        var environment = await AppHostModel.EnvironmentExpressionsAsync(resources.EmployeeApi.Resource);

        ConnectionStrings(environment).Should().BeEquivalentTo(new Dictionary<string, string>
        {
            [WriteKey] = $"{ServerPrefix};Application Name=employee-api-write",
            [ReadKey] = $"{ServerPrefix};Application Name=employee-api-read;Options=-c default_transaction_read_only=on",
        });
    }

    [Fact]
    public void Migrations_WaitsUntilEmployeeDatabaseIsHealthy()
    {
        var (_, resources) = AppHostModel.Create();

        var waits = Waits(resources.EmployeeMigrations.Resource);

        waits.Should().Contain(("employee-db", WaitType.WaitUntilHealthy));
        waits.Should().NotContain(wait => wait.Type == WaitType.WaitForCompletion);
    }

    [Fact]
    public void Api_WaitsForDatabaseAndSuccessfulMigrationCompletion()
    {
        var (_, resources) = AppHostModel.Create();

        var completion = resources.EmployeeApi.Resource.Annotations.OfType<WaitAnnotation>()
            .Where(wait => wait.WaitType == WaitType.WaitForCompletion)
            .ToList();

        Waits(resources.EmployeeApi.Resource).Should().Contain(("employee-db", WaitType.WaitUntilHealthy));
        completion.Should().ContainSingle();
        (completion[0].Resource, completion[0].ExitCode).Should().Be((resources.EmployeeMigrations.Resource, 0));
    }

    [Fact]
    public void Api_HasHttpHealthCheckOnReadyPath()
    {
        var (_, resources) = AppHostModel.Create();

        var checks = resources.EmployeeApi.Resource.Annotations.OfType<HealthCheckAnnotation>().Select(check => check.Key);

        checks.Should().Equal($"employee-api_http_{HealthEndpoints.ReadyPath}_200_check");
        EmergencyHubApplication.ApiReadyHealthPath.Should().Be(HealthEndpoints.ReadyPath, "ServiceDefaults의 ready 경로와 같아야 한다");
    }

    // ---- 실패 ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AddEmergencyHub_BlankImageTag_Throws(string? tag)
    {
        var builder = AppHostModel.CreateBuilder();

        var act = () => builder.AddEmergencyHub(tag!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Services_HaveNoWithReferenceToDatabaseOrSuperuserCredentials()
    {
        // WithReference(db)는 ConnectionStrings__employee-db(슈퍼유저 postgres 자격 증명)를 넣는다(ADR-0011).
        var (_, resources) = AppHostModel.Create();

        foreach (var service in new IResource[] { resources.EmployeeMigrations.Resource, resources.EmployeeApi.Resource })
        {
            var environment = await AppHostModel.EnvironmentExpressionsAsync(service);

            environment.Keys.Should().NotContain(key => key.StartsWith("ConnectionStrings__", StringComparison.Ordinal) && key != WriteKey && key != ReadKey, service.Name);
            environment.Values.Should().NotContain(value => value.Contains("postgres-password", StringComparison.Ordinal), service.Name);
            environment.Values.Should().NotContain(value => value.Contains("Username=postgres", StringComparison.Ordinal), service.Name);
            service.Annotations.OfType<ResourceRelationshipAnnotation>()
                .Should().NotContain(relation => relation.Type == "Reference" && relation.Resource.Name == "employee-db", service.Name);
        }
    }

    [Fact]
    public void AddEmergencyHub_CalledTwice_ThrowsForDuplicateResourceNames()
    {
        var (builder, _) = AppHostModel.Create();

        var act = () => builder.AddEmergencyHub(AppHostModel.PostgresImageTag);

        act.Should().Throw<DistributedApplicationException>();
    }

    // ---- 엣지 ----

    [Fact]
    public async Task ConnectionStrings_ReadOnlyOptionOnlyInReadAndNoForbiddenKeywords()
    {
        var (_, resources) = AppHostModel.Create();

        var migrations = await AppHostModel.EnvironmentExpressionsAsync(resources.EmployeeMigrations.Resource);
        var api = await AppHostModel.EnvironmentExpressionsAsync(resources.EmployeeApi.Resource);
        string[] all = [migrations[WriteKey], api[WriteKey], api[ReadKey]];

        migrations[WriteKey].Should().NotContain("Options=");
        api[WriteKey].Should().NotContain("Options=");
        api[ReadKey].Should().EndWith(";Options=-c default_transaction_read_only=on");
        all.Should().AllSatisfy(value => value.Should().NotContainAny("Include Error Detail", "Persist Security Info", "Timeout"));
    }

    [Fact]
    public void Services_RunAsSingleInstanceEach()
    {
        var (_, resources) = AppHostModel.Create();

        resources.EmployeeMigrations.Resource.Annotations.OfType<ReplicaAnnotation>().Should().BeEmpty("마이그레이션 적용 주체는 하나다(TD-011)");
        resources.EmployeeMigrations.Resource.Annotations.OfType<HealthCheckAnnotation>().Should().BeEmpty("Worker라 HTTP 헬스가 없고 완료(종료 코드)로 판단한다");
        resources.EmployeeApi.Resource.Annotations.OfType<ReplicaAnnotation>().Should().BeEmpty();
    }

    [Fact]
    public void Api_DoesNotWaitOnItselfOrForCompletionOfDatabase()
    {
        var (_, resources) = AppHostModel.Create();

        var waits = Waits(resources.EmployeeApi.Resource);

        waits.Should().NotContain(wait => wait.Resource == "employee-api");
        waits.Should().NotContain(("employee-db", WaitType.WaitForCompletion));
    }

    private static Dictionary<string, string> ConnectionStrings(Dictionary<string, string> environment) =>
        environment.Where(pair => pair.Key.StartsWith("ConnectionStrings__", StringComparison.Ordinal)).ToDictionary();

    private static List<(string Resource, WaitType Type)> Waits(IResource resource) =>
        resource.Annotations.OfType<WaitAnnotation>().Select(wait => (wait.Resource.Name, wait.WaitType)).ToList();
}
