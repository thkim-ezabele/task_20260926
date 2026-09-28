using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.QueryPlans;

/// <summary>
/// EF Core가 실제로 보낸 명령 하나(SQL 원문 · 매개변수 사본)입니다. <see cref="QueryPlan"/>이 같은 SQL · 매개변수로 EXPLAIN을 실행합니다.
/// </summary>
/// <param name="Text">EF Core가 만든 SQL 원문.</param>
/// <param name="Parameters">매개변수 사본(이름 · 형식 · 값 유지).</param>
public sealed record CapturedCommand(string Text, IReadOnlyList<NpgsqlParameter> Parameters);
