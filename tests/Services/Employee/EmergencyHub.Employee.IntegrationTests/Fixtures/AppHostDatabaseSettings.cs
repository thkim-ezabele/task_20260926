namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>AppHost <c>EmployeeDatabaseSettings.cs</c>에서 읽은 값입니다. 생성 스크립트는 보간을 풀어 둔 최종 문장입니다.</summary>
/// <param name="DatabaseName">Database 이름.</param>
/// <param name="AppRoleName">애플리케이션 롤 이름.</param>
/// <param name="CreationScript">생성 스크립트(보간 결과).</param>
/// <param name="AppRolePasswordVariable">롤 비밀번호 환경 변수 이름.</param>
public sealed record AppHostDatabaseSettings(string DatabaseName, string AppRoleName, string CreationScript, string AppRolePasswordVariable);
