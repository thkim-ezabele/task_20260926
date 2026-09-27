namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// fixture가 재현하는 AppHost Employee DB 구성 값입니다(database.md "로컬 DB 구성 (AppHost)").
/// AppHost <c>EmployeeDatabaseSettings</c>는 AppHost 어셈블리의 internal이라 참조하지 않고, 같은 값인지 원본 파일과 대조합니다
/// (<see cref="AppHostDatabaseSettingsSource"/>, 테스트 <c>AppHostDatabaseSettingsSourceTests</c>).
/// </summary>
public static class EmployeeDatabaseSettings
{
    /// <summary>PostgreSQL Database 이름입니다.</summary>
    public const string DatabaseName = "emergency_hub_employee";

    /// <summary>애플리케이션 롤입니다(초기화 스크립트가 만들고 Database 소유자, 슈퍼유저 아님).</summary>
    public const string AppRoleName = "employee_app";

    /// <summary>Database 생성 스크립트입니다. 슈퍼유저 연결로 한 번만 실행합니다(AppHost <c>WithCreationScript</c>와 같은 한 문장).</summary>
    public const string CreationScript = $"CREATE DATABASE {DatabaseName} OWNER {AppRoleName}";

    /// <summary>초기화 스크립트가 롤 비밀번호를 받는 컨테이너 환경 변수입니다.</summary>
    public const string AppRolePasswordVariable = "EMPLOYEE_APP_PASSWORD";

    /// <summary>postgres 공식 이미지가 초기화 스크립트를 실행하는 컨테이너 폴더입니다.</summary>
    public const string InitScriptsTargetDirectory = "/docker-entrypoint-initdb.d/";
}
