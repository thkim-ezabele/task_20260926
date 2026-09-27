namespace EmergencyHub.AppHost;

/// <summary>
/// 로컬 애플리케이션 모델을 구성합니다(ADR-0011, ADR-0012, database.md "로컬 DB 구성 (AppHost)").
/// postgres → employee-db(생성 스크립트) → employee-migrations(종료 코드 0) → employee-api(Healthy) 순서로 뜹니다.
/// </summary>
internal static class EmergencyHubApplication
{
    /// <summary>Api 준비 헬스 경로입니다. ServiceDefaults <c>HealthEndpoints.ReadyPath</c>와 같습니다(AppHost는 ServiceDefaults를 참조하지 않음).</summary>
    internal const string ApiReadyHealthPath = "/health/ready";

    /// <summary>Api 헬스 검사에 쓰는 엔드포인트입니다(Api launchSettings의 http 프로필).</summary>
    internal const string ApiHealthEndpointName = "http";

    /// <summary>비밀번호 생성 최소 길이입니다(영문 대소문자 · 숫자, 약 185비트).</summary>
    internal const int PasswordMinLength = 32;

    /// <summary>리소스를 추가합니다.</summary>
    /// <param name="builder">분산 애플리케이션 빌더.</param>
    /// <param name="postgresImageTag">PostgreSQL 이미지 태그(<see cref="PostgresImageTag.Read"/>).</param>
    /// <returns>추가한 리소스 빌더.</returns>
    /// <exception cref="ArgumentException"><paramref name="postgresImageTag"/>가 비어 있는 경우.</exception>
    /// <exception cref="InvalidOperationException">AppHost 디렉터리에 초기화 스크립트 폴더가 없는 경우.</exception>
    /// <remarks>
    /// <c>WithReference</c>는 쓰지 않습니다. Database 리소스의 연결 식은 슈퍼유저 자격 증명이라 <c>employee_app</c> 연결 식을 직접 넣습니다.
    /// 비밀번호 매개변수는 설정(user-secrets)에 값이 없으면 첫 실행 때 생성해 AppHost user-secrets에 저장합니다(<c>persist</c>).
    /// </remarks>
    internal static EmergencyHubResources AddEmergencyHub(this IDistributedApplicationBuilder builder, string postgresImageTag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresImageTag);

        var postgresPasswordParameter = AddPassword(builder, EmergencyHubResourceNames.PostgresPassword);
        var appPasswordParameter = AddPassword(builder, EmergencyHubResourceNames.EmployeeAppPassword);

        var postgres = builder.AddPostgres(EmergencyHubResourceNames.Postgres, password: postgresPasswordParameter)
            .WithImageTag(postgresImageTag)
            .WithDataVolume(EmployeeDatabaseSettings.DataVolumeName)
            .WithInitFiles(EmployeeDatabaseSettings.InitFilesDirectory)
            .WithEnvironment(EmployeeDatabaseSettings.AppRolePasswordVariable, appPasswordParameter);

        var employeeDatabase = postgres.AddDatabase(EmergencyHubResourceNames.EmployeeDatabase, EmployeeDatabaseSettings.DatabaseName)
            .WithCreationScript(EmployeeDatabaseSettings.CreationScript);

        var server = postgres.Resource.PrimaryEndpoint;

        var migrations = builder.AddProject<Projects.EmergencyHub_Employee_MigrationService>(EmergencyHubResourceNames.EmployeeMigrations)
            .WithEnvironment(
                EmployeeConnectionStrings.WriteVariable,
                EmployeeConnectionStrings.Write(server, appPasswordParameter.Resource, EmployeeConnectionStrings.MigrationApplicationName))
            .WaitFor(employeeDatabase);

        var api = builder.AddProject<Projects.EmergencyHub_Employee_Api>(EmergencyHubResourceNames.EmployeeApi)
            .WithEnvironment(
                EmployeeConnectionStrings.WriteVariable,
                EmployeeConnectionStrings.Write(server, appPasswordParameter.Resource, EmployeeConnectionStrings.ApiWriteApplicationName))
            .WithEnvironment(
                EmployeeConnectionStrings.ReadVariable,
                EmployeeConnectionStrings.Read(server, appPasswordParameter.Resource, EmployeeConnectionStrings.ApiReadApplicationName))
            .WaitFor(employeeDatabase)
            .WaitForCompletion(migrations)
            .WithHttpHealthCheck(ApiReadyHealthPath, endpointName: ApiHealthEndpointName);

        return new EmergencyHubResources(postgres, employeeDatabase, migrations, api);
    }

    private static IResourceBuilder<ParameterResource> AddPassword(IDistributedApplicationBuilder builder, string name) =>
        builder.AddParameter(name, new GenerateParameterDefault { MinLength = PasswordMinLength, Special = false }, secret: true, persist: true);
}
