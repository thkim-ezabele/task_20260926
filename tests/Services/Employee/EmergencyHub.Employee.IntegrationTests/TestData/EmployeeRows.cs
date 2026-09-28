using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.TestData;

/// <summary><c>employees</c> 테이블 상태를 운영 코드 없이 SQL로 확인합니다(S06-T06 "실패 때 DB 0건", "먼저 커밋된 쪽만 남음").</summary>
public static class EmployeeRows
{
    /// <summary>저장된 직원 행 수입니다(쓰기 연결).</summary>
    /// <param name="database">컬렉션 fixture.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>행 수.</returns>
    public static async Task<long> CountAsync(EmployeeDatabaseFixture database, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        await using var connection = await database.OpenWriteConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT count(*) FROM employees", connection);
        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    }

    /// <summary>저장된 직원의 ID와 정규화 이메일입니다(ID 순서).</summary>
    /// <param name="database">컬렉션 fixture.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>(ID, 정규화 이메일) 목록.</returns>
    public static async Task<IReadOnlyList<(Guid Id, string NormalizedEmail)>> ListAsync(EmployeeDatabaseFixture database, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        await using var connection = await database.OpenWriteConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id, normalized_email FROM employees ORDER BY id", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<(Guid, string)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetGuid(0), reader.GetString(1)));
        }

        return rows;
    }
}
