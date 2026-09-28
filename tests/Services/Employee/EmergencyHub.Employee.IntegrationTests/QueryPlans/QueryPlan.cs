using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.QueryPlans;

/// <summary>
/// 가로챈 명령(<see cref="CapturedCommand"/>)을 같은 SQL · 매개변수로 <c>EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, SUMMARY OFF)</c> 실행해 계획 줄을 돌려줍니다.
/// </summary>
/// <remarks>
/// 매개변수를 서버에 바인딩한 채로 계획을 세우므로 EF Core 실행과 같은 조건(사용자 지정 계획)입니다. 원시 SQL은 테스트 확인용이며 Repository에는 쓰지 않습니다.
/// </remarks>
public static class QueryPlan
{
    /// <summary>EXPLAIN 앞머리입니다(dba 기대 형태 실측과 같은 옵션).</summary>
    public const string ExplainPrefix = "EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, SUMMARY OFF) ";

    /// <summary>계획을 줄 단위로 읽습니다.</summary>
    /// <param name="connection">열린 연결(읽기 쿼리는 읽기 연결, 쓰기 연결 쿼리는 쓰기 연결).</param>
    /// <param name="command">가로챈 명령.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>계획 줄.</returns>
    public static async Task<IReadOnlyList<string>> ExplainAsync(NpgsqlConnection connection, CapturedCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(command);

        await using var explain = new NpgsqlCommand(ExplainPrefix + command.Text, connection);
        foreach (var parameter in command.Parameters)
        {
            explain.Parameters.Add(parameter.Clone());
        }

        var lines = new List<string>();
        await using var reader = await explain.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            lines.Add(reader.GetString(0));
        }

        return lines;
    }
}
