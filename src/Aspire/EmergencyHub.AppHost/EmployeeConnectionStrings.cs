namespace EmergencyHub.AppHost;

/// <summary>
/// <c>WithReference</c> 없이 조립하는 Employee 연결 식입니다(ADR-0011, database.md "로컬 DB 구성 (AppHost)" 연결 식 표).
/// 호스트 · 포트는 서버 엔드포인트에서 얻고, 계정은 <c>employee_app</c>입니다(슈퍼유저 자격 증명을 넣지 않음).
/// </summary>
internal static class EmployeeConnectionStrings
{
    /// <summary>쓰기 연결 환경 변수입니다(<c>ConnectionStrings:Write</c>, ADR-0009).</summary>
    internal const string WriteVariable = "ConnectionStrings__Write";

    /// <summary>읽기 연결 환경 변수입니다(<c>ConnectionStrings:Read</c>, ADR-0009).</summary>
    internal const string ReadVariable = "ConnectionStrings__Read";

    /// <summary>MigrationService 쓰기 연결의 <c>Application Name</c>입니다.</summary>
    internal const string MigrationApplicationName = "employee-migration";

    /// <summary>Api 쓰기 연결의 <c>Application Name</c>입니다.</summary>
    internal const string ApiWriteApplicationName = "employee-api-write";

    /// <summary>Api 읽기 연결의 <c>Application Name</c>입니다.</summary>
    internal const string ApiReadApplicationName = "employee-api-read";

    /// <summary>읽기 연결에만 붙이는 세션 옵션입니다. 안전장치이지 보안 경계가 아닙니다(ADR-0011).</summary>
    internal const string ReadOnlyOptions = "-c default_transaction_read_only=on";

    /// <summary>쓰기 연결 식을 만듭니다.</summary>
    /// <param name="server">PostgreSQL 서버의 기본 엔드포인트.</param>
    /// <param name="password"><c>employee_app</c> 비밀번호 매개변수.</param>
    /// <param name="applicationName"><c>Application Name</c> 값.</param>
    /// <returns>연결 식.</returns>
    internal static ReferenceExpression Write(EndpointReference server, ParameterResource password, string applicationName) =>
        ReferenceExpression.Create(
            $"Host={server.Property(EndpointProperty.Host)};Port={server.Property(EndpointProperty.Port)};Database={EmployeeDatabaseSettings.DatabaseName};Username={EmployeeDatabaseSettings.AppRoleName};Password={password};Application Name={applicationName}");

    /// <summary>읽기 연결 식을 만듭니다. 쓰기 식과 같고 끝에 <see cref="ReadOnlyOptions"/>를 붙입니다.</summary>
    /// <param name="server">PostgreSQL 서버의 기본 엔드포인트.</param>
    /// <param name="password"><c>employee_app</c> 비밀번호 매개변수.</param>
    /// <param name="applicationName"><c>Application Name</c> 값.</param>
    /// <returns>연결 식.</returns>
    internal static ReferenceExpression Read(EndpointReference server, ParameterResource password, string applicationName) =>
        ReferenceExpression.Create($"{Write(server, password, applicationName)};Options={ReadOnlyOptions}");
}
