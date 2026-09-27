namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>컨테이너 안 <c>psql</c> 실행 결과입니다(<see cref="EmployeeDatabaseFixture.ExecutePsqlScriptAsync"/>).</summary>
/// <param name="ExitCode">종료 코드(<c>ON_ERROR_STOP=1</c>이라 SQL 오류면 0이 아님).</param>
/// <param name="Stdout">표준 출력.</param>
/// <param name="Stderr">표준 오류(<c>NOTICE</c> · <c>ERROR</c> 문구).</param>
public sealed record PsqlResult(long ExitCode, string Stdout, string Stderr);
