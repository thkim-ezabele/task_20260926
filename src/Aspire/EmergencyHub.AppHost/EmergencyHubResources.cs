namespace EmergencyHub.AppHost;

/// <summary><see cref="EmergencyHubApplication.AddEmergencyHub"/>가 추가한 리소스 빌더입니다.</summary>
/// <param name="Postgres">PostgreSQL 서버.</param>
/// <param name="EmployeeDatabase">Employee Database.</param>
/// <param name="EmployeeMigrations">Employee MigrationService.</param>
/// <param name="EmployeeApi">Employee Api.</param>
internal sealed record EmergencyHubResources(
    IResourceBuilder<PostgresServerResource> Postgres,
    IResourceBuilder<PostgresDatabaseResource> EmployeeDatabase,
    IResourceBuilder<ProjectResource> EmployeeMigrations,
    IResourceBuilder<ProjectResource> EmployeeApi);
