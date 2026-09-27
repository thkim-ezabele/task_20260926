namespace EmergencyHub.AppHost;

/// <summary>AppHost 리소스 · 매개변수 이름입니다(ADR-0011 리소스 표). user-secrets 키는 <c>Parameters:&lt;매개변수 이름&gt;</c>입니다.</summary>
internal static class EmergencyHubResourceNames
{
    /// <summary>PostgreSQL 서버 리소스입니다.</summary>
    internal const string Postgres = "postgres";

    /// <summary>Employee Database 리소스입니다.</summary>
    internal const string EmployeeDatabase = "employee-db";

    /// <summary>Employee MigrationService 리소스입니다(ADR-0011 · ADR-0012).</summary>
    internal const string EmployeeMigrations = "employee-migrations";

    /// <summary>Employee Api 리소스입니다.</summary>
    internal const string EmployeeApi = "employee-api";

    /// <summary>슈퍼유저 <c>postgres</c> 비밀번호 매개변수입니다(서버 초기화 · 생성 스크립트 전용).</summary>
    internal const string PostgresPassword = "postgres-password";

    /// <summary><c>employee_app</c> 롤 비밀번호 매개변수입니다.</summary>
    internal const string EmployeeAppPassword = "employee-app-password";
}
