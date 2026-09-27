namespace EmergencyHub.AppHost;

/// <summary>
/// Employee DB 구성 값입니다(database.md "로컬 DB 구성 (AppHost)", ADR-0011). 초기화 스크립트 · 통합 테스트 fixture(S03-T06)와 같은 이름을 씁니다.
/// </summary>
internal static class EmployeeDatabaseSettings
{
    /// <summary>PostgreSQL Database 이름입니다(리소스 이름 <c>employee-db</c>와 분리, ASPIRE006).</summary>
    internal const string DatabaseName = "emergency_hub_employee";

    /// <summary>애플리케이션 롤입니다. 초기화 스크립트가 만들고 Database 소유자가 됩니다(슈퍼유저 아님).</summary>
    internal const string AppRoleName = "employee_app";

    /// <summary>Database 생성 스크립트입니다. 한 문장만 둡니다(재시작 때 42P04는 Aspire가 무시).</summary>
    internal const string CreationScript = $"CREATE DATABASE {DatabaseName} OWNER {AppRoleName}";

    /// <summary>이름 있는 데이터 볼륨입니다. 복구 때는 이 볼륨만 지웁니다.</summary>
    internal const string DataVolumeName = "emergency-hub-postgres-data";

    /// <summary>초기화 스크립트 폴더입니다(AppHost 디렉터리 기준 경로, 폴더 전체가 <c>/docker-entrypoint-initdb.d</c>로 복사됨).</summary>
    internal const string InitFilesDirectory = "postgres-init";

    /// <summary>초기화 스크립트가 롤 비밀번호를 받는 서버 컨테이너 환경 변수입니다.</summary>
    internal const string AppRolePasswordVariable = "EMPLOYEE_APP_PASSWORD";
}
