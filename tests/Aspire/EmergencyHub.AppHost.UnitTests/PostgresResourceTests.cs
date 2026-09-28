using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace EmergencyHub.AppHost.UnitTests;

// S03-T05 · database.md "로컬 DB 구성 (AppHost)": 서버(이미지 태그 · 볼륨 · 초기화 스크립트 · 롤 비밀번호 전달), Database(이름 · 생성 스크립트 한 문장), 매개변수 2개.
[Trait("FR", "PRD-001/FR-03")]
[Trait("NFR", "PRD-001/NFR-06")]
public sealed class PostgresResourceTests
{
    private const string PostgresPasswordExpression = "{postgres-password.value}";
    private const string AppPasswordExpression = "{employee-app-password.value}";

    // ---- 성공 ----

    [Fact]
    public void Postgres_UsesPostgresImageWithTagFromBuildMetadata()
    {
        var (_, resources) = AppHostModel.Create();

        var image = resources.Postgres.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();

        (image.Registry, image.Image, image.Tag).Should().Be(("docker.io", "library/postgres", RepositoryFiles.BuildProperty("EmergencyHubPostgresImageTag")));
    }

    [Fact]
    public void Postgres_MountsNamedDataVolume()
    {
        var (_, resources) = AppHostModel.Create();

        var volumes = resources.Postgres.Resource.Annotations.OfType<ContainerMountAnnotation>().Where(mount => mount.Type == ContainerMountType.Volume).ToList();

        volumes.Should().ContainSingle();
        (volumes[0].Source, volumes[0].Target, volumes[0].IsReadOnly).Should().Be(("emergency-hub-postgres-data", "/var/lib/postgresql/data", false));
        EmployeeDatabaseSettings.DataVolumeName.Should().Be("emergency-hub-postgres-data");
    }

    [Fact]
    public void Postgres_CopiesInitFilesToDockerEntrypointDirectory()
    {
        var (_, resources) = AppHostModel.Create();

        var initFiles = resources.Postgres.Resource.Annotations.OfType<ContainerFileSystemCallbackAnnotation>().ToList();

        initFiles.Should().ContainSingle().Which.DestinationPath.Should().Be("/docker-entrypoint-initdb.d");
    }

    [Fact]
    public async Task Postgres_PassesSuperuserAndAppRolePasswordsAsParameters()
    {
        var (_, resources) = AppHostModel.Create();

        var environment = await AppHostModel.EnvironmentExpressionsAsync(resources.Postgres.Resource);

        environment.Should().Contain("POSTGRES_USER", "postgres");
        environment.Should().Contain("POSTGRES_PASSWORD", PostgresPasswordExpression);
        environment.Should().Contain("EMPLOYEE_APP_PASSWORD", AppPasswordExpression);
    }

    [Fact]
    public void EmployeeDatabase_UsesDatabaseNameAndSingleOwnerCreationScript()
    {
        var (_, resources) = AppHostModel.Create();

        var database = resources.EmployeeDatabase.Resource;
        var script = AppHostModel.CreationScripts(database).Single();

        (database.Name, database.DatabaseName, database.Parent).Should().Be(("employee-db", "emergency_hub_employee", resources.Postgres.Resource));
        script.Should().Be("CREATE DATABASE emergency_hub_employee OWNER employee_app");
    }

    [Theory]
    [InlineData("postgres-password")]
    [InlineData("employee-app-password")]
    public void Parameters_RunMode_AreSecretAndPersistedToUserSecrets(string name)
    {
        var (builder, _) = AppHostModel.Create();

        var parameter = AppHostModel.Parameter(builder, name);

        parameter.Secret.Should().BeTrue();
        parameter.Default!.GetType().Name.Should().Be("UserSecretsParameterDefault", "persist: true면 실행 모드에서 생성 값을 AppHost user-secrets에 저장한다(9.5.2 internal 형식이라 이름으로 비교)");
    }

    [Theory]
    [InlineData("postgres-password")]
    [InlineData("employee-app-password")]
    public void Parameters_GenerateThirtyTwoAlphanumericCharacters(string name)
    {
        // 게시 모드에는 user-secrets 래퍼가 없어 생성 규칙을 그대로 볼 수 있다(값은 만들지 않는다).
        var (builder, _) = AppHostModel.Create(DistributedApplicationOperation.Publish);

        var generate = AppHostModel.Parameter(builder, name).Default.Should().BeOfType<GenerateParameterDefault>().Subject;

        (generate.MinLength, generate.Special, generate.Lower, generate.Upper, generate.Numeric).Should().Be((32, false, true, true, true));
    }

    // ---- 실패 ----

    [Fact]
    public void AddEmergencyHub_InitFilesDirectoryMissingUnderAppHostDirectory_Throws()
    {
        // 초기화 스크립트 폴더는 AppHost 디렉터리 기준 경로다. 다른 디렉터리(테스트 어셈블리)가 기준이면 구성 단계에서 실패한다.
        var builder = AppHostModel.CreateBuilder(assemblyName: null);

        var act = () => builder.AddEmergencyHub(AppHostModel.PostgresImageTag);

        act.Should().Throw<InvalidOperationException>().WithMessage("*postgres-init*");
    }

    [Fact]
    public async Task Postgres_DoesNotExposeAppRolePasswordUnderOtherKeys()
    {
        var (_, resources) = AppHostModel.Create();

        var environment = await AppHostModel.EnvironmentExpressionsAsync(resources.Postgres.Resource);

        environment.Where(pair => pair.Value.Contains(AppPasswordExpression, StringComparison.Ordinal)).Select(pair => pair.Key)
            .Should().Equal("EMPLOYEE_APP_PASSWORD");
    }

    // ---- 엣지 ----

    [Fact]
    public void InitFilesDirectory_ContainsOnlyShellScripts()
    {
        // 폴더 전체가 /docker-entrypoint-initdb.d로 복사되므로 .sh 외 파일(README 등)을 두지 않는다.
        var directory = Path.Combine(RepositoryFiles.AppHostDirectory, EmployeeDatabaseSettings.InitFilesDirectory);

        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Select(Path.GetFileName).ToList();

        files.Should().Equal("01-create-employee-app-role.sh");
    }

    [Fact]
    public void CreationScript_UsesSameDatabaseAndRoleNamesAsConnectionStrings()
    {
        EmployeeDatabaseSettings.CreationScript.Should().Be($"CREATE DATABASE {EmployeeDatabaseSettings.DatabaseName} OWNER {EmployeeDatabaseSettings.AppRoleName}");
        (EmployeeDatabaseSettings.DatabaseName, EmployeeDatabaseSettings.AppRoleName).Should().Be(("emergency_hub_employee", "employee_app"));
    }

    [Fact]
    public void Postgres_HasNoCreationScriptOrReplicaOfItsOwn()
    {
        var (_, resources) = AppHostModel.Create();

        AppHostModel.CreationScripts(resources.Postgres.Resource).Should().BeEmpty("생성 스크립트는 Database 리소스에만 둔다");
        resources.Postgres.Resource.Annotations.OfType<ReplicaAnnotation>().Should().BeEmpty();
    }
}
