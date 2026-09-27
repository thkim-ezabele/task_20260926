using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>
/// 테스트 전용 트리거 생성 · 정리 도우미입니다(testing-strategy.md "장애 주입"). 실제 서버 오류가 필요한 곳(P1 관찰 · P2 커밋 시점 40001 · P6 매번 40001)에 씁니다.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>employee_app</c> 쓰기 연결로 만듭니다(슈퍼유저 불필요). <c>Respawner.CreateAsync</c> 뒤(fixture 준비 뒤)에만 만듭니다.</description></item>
/// <item><description>테스트 본문은 <c>await using var trigger = await TestTriggers.CreateXxxAsync(Database, ct);</c>로 감싸 폐기 때 지웁니다(finally DROP).
/// 폐기 뒤 <see cref="CountLeftoversAsync"/>가 <see cref="TestObjectCounts.None"/>인지 단언합니다.</description></item>
/// <item><description>같은 컬렉션에서 순차 실행합니다. 지우지 않으면 뒤 테스트의 <c>INSERT</c>가 모두 실패합니다.</description></item>
/// </list>
/// </remarks>
public static class TestTriggers
{
    /// <summary>P2: 첫 커밋에서만 40001을 내는 지연 제약 트리거를 만듭니다.</summary>
    /// <param name="database">fixture.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>만든 트리거(폐기 때 정리).</returns>
    public static Task<TestTrigger> CreateCommitFailureOnceAsync(EmployeeDatabaseFixture database, CancellationToken cancellationToken) =>
        CreateAsync(database, TestTriggerSql.CreateCommitFailureOnce, TestTriggerSql.CommitFailureOnceAttempts, cancellationToken);

    /// <summary>P6: 모든 <c>INSERT</c>에서 40001을 내는 트리거를 만듭니다.</summary>
    /// <param name="database">fixture.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>만든 트리거(폐기 때 정리).</returns>
    public static Task<TestTrigger> CreateAlwaysFailAsync(EmployeeDatabaseFixture database, CancellationToken cancellationToken) =>
        CreateAsync(database, TestTriggerSql.CreateAlwaysFail, TestTriggerSql.AlwaysFailAttempts, cancellationToken);

    /// <summary>P1: <c>INSERT</c> 때 서버 트랜잭션 설정을 기록하는 관찰 트리거를 만듭니다.</summary>
    /// <param name="database">fixture.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>만든 트리거(폐기 때 정리).</returns>
    public static Task<TestTrigger> CreateIsolationProbeAsync(EmployeeDatabaseFixture database, CancellationToken cancellationToken) =>
        CreateAsync(database, TestTriggerSql.CreateIsolationProbe, attemptsSequence: null, cancellationToken);

    /// <summary>남은 테스트 전용 객체 수를 셉니다.</summary>
    /// <param name="database">fixture.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>트리거 · 함수 · 릴레이션 수.</returns>
    public static async Task<TestObjectCounts> CountLeftoversAsync(EmployeeDatabaseFixture database, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        await using var connection = await database.OpenWriteConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(TestTriggerSql.CountLeftovers, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return new TestObjectCounts(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2));
    }

    internal static async Task DropAllAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new NpgsqlCommand(TestTriggerSql.DropAll, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static async Task<NpgsqlConnection> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task<TestTrigger> CreateAsync(
        EmployeeDatabaseFixture database,
        string createSql,
        string? attemptsSequence,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        var trigger = new TestTrigger(database.WriteConnectionString, attemptsSequence);
        try
        {
            await using var connection = await database.OpenWriteConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(createSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return trigger;
        }
        catch
        {
            // 일부만 만들어졌어도 지운다(뒤 테스트 보호).
            await trigger.DisposeAsync();
            throw;
        }
    }
}
