namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T06: fixture의 DB 이름 · 롤 · 생성 스크립트 · 비밀번호 환경 변수는 AppHost EmployeeDatabaseSettings와 같아야 한다(원본 파일 대조),
// 초기화 스크립트는 AppHost postgres-init 폴더의 원본을 그대로 마운트한다(복사본 없음).
[Trait("FR", "PRD-001/FR-09")]
public sealed class AppHostDatabaseSettingsSourceTests
{
    private const string SampleSource = """
        internal static class EmployeeDatabaseSettings
        {
            internal const string DatabaseName = "sample_db";
            internal const string AppRoleName = "sample_app";
            internal const string CreationScript = $"CREATE DATABASE {DatabaseName} OWNER {AppRoleName}";
            internal const string AppRolePasswordVariable = "SAMPLE_PASSWORD";
        }
        """;

    // ---- 성공 ----

    [Fact]
    public void Read_AppHostSource_MatchesFixtureSettings()
    {
        var appHost = AppHostDatabaseSettingsSource.Read();

        appHost.Should().Be(new AppHostDatabaseSettings(
            EmployeeDatabaseSettings.DatabaseName,
            EmployeeDatabaseSettings.AppRoleName,
            EmployeeDatabaseSettings.CreationScript,
            EmployeeDatabaseSettings.AppRolePasswordVariable));
    }

    [Fact]
    public void PostgresInitDirectory_ContainsOnlyTheAppHostRoleScript()
    {
        var scripts = Directory.EnumerateFiles(RepositoryFiles.PostgresInitDirectory).Select(Path.GetFileName);

        scripts.Should().Equal("01-create-employee-app-role.sh");
    }

    [Fact]
    public void Parse_Sample_ResolvesInterpolatedCreationScript()
    {
        var settings = AppHostDatabaseSettingsSource.Parse(SampleSource);

        settings.Should().Be(new AppHostDatabaseSettings("sample_db", "sample_app", "CREATE DATABASE sample_db OWNER sample_app", "SAMPLE_PASSWORD"));
    }

    // ---- 실패 ----

    [Fact]
    public void Parse_MissingConstant_Throws()
    {
        var act = () => AppHostDatabaseSettingsSource.Parse(SampleSource.Replace("AppRolePasswordVariable", "Other", StringComparison.Ordinal));

        act.Should().Throw<InvalidOperationException>().WithMessage("*AppRolePasswordVariable*");
    }

    [Fact]
    public void Parse_ChangedDatabaseName_DiffersFromFixtureCreationScript()
    {
        var settings = AppHostDatabaseSettingsSource.Parse(SampleSource);

        settings.CreationScript.Should().NotBe(EmployeeDatabaseSettings.CreationScript);
    }

    // ---- 엣지 ----

    [Fact]
    public void Parse_UnknownPlaceholder_Throws()
    {
        var act = () => AppHostDatabaseSettingsSource.Parse(SampleSource.Replace("{AppRoleName}", "{Owner}", StringComparison.Ordinal));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Owner*");
    }

    [Fact]
    public void Parse_DuplicatedConstant_Throws()
    {
        var act = () => AppHostDatabaseSettingsSource.Parse(SampleSource + "\nconst string DatabaseName = \"again\";");

        act.Should().Throw<InvalidOperationException>().WithMessage("*DatabaseName*");
    }
}
