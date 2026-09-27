using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>검증 쿼리(testing-strategy.md "DB 검증 쿼리")를 짧게 쓰는 도우미입니다. 값은 매개변수로만 넘깁니다.</summary>
public static class NpgsqlConnectionExtensions
{
    /// <summary>스칼라 값 하나를 읽습니다.</summary>
    /// <typeparam name="T">결과 형식(예: <c>count(*)</c>는 <see cref="long"/>).</typeparam>
    /// <param name="connection">열린 연결.</param>
    /// <param name="sql">SQL.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <param name="parameters">위치 매개변수(<c>$1</c>, <c>$2</c> ...).</param>
    /// <returns>결과 값.</returns>
    public static async Task<T> ScalarAsync<T>(this NpgsqlConnection connection, string sql, CancellationToken cancellationToken, params object[] parameters)
    {
        await using var command = Create(connection, sql, parameters);
        return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    /// <summary>결과 없는 SQL을 실행합니다.</summary>
    /// <param name="connection">열린 연결.</param>
    /// <param name="sql">SQL.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <param name="parameters">위치 매개변수(<c>$1</c>, <c>$2</c> ...).</param>
    /// <returns>영향받은 행 수.</returns>
    public static async Task<int> ExecuteSqlAsync(this NpgsqlConnection connection, string sql, CancellationToken cancellationToken, params object[] parameters)
    {
        await using var command = Create(connection, sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static NpgsqlCommand Create(NpgsqlConnection connection, string sql, object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var command = new NpgsqlCommand(sql, connection);
        foreach (var value in parameters)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        }

        return command;
    }
}
