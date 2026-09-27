using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>
/// 만든 테스트 전용 트리거 하나입니다. <c>await using</c>으로 쓰면 폐기 때 모든 테스트 객체를 지웁니다(testing-strategy.md "트리거 규칙": try/finally DROP).
/// </summary>
/// <remarks>정리 뒤 잔여 검사는 <see cref="TestTriggers.CountLeftoversAsync"/>로 단언합니다.</remarks>
public sealed class TestTrigger : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly string? _attemptsSequence;
    private bool _disposed;

    internal TestTrigger(string connectionString, string? attemptsSequence)
    {
        _connectionString = connectionString;
        _attemptsSequence = attemptsSequence;
    }

    /// <summary>
    /// 트리거 실행 횟수(= 시도 수)입니다. 시퀀스는 트랜잭션 롤백과 무관하게 증가합니다(P2 기대 2, P6 기대 <c>MaxRetryCount + 1</c>).
    /// </summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>시도 수. 한 번도 실행되지 않았으면 0.</returns>
    /// <exception cref="InvalidOperationException">시도 수를 세지 않는 트리거(P1 관찰)인 경우.</exception>
    public async Task<long> ReadAttemptsAsync(CancellationToken cancellationToken)
    {
        var sequence = _attemptsSequence ?? throw new InvalidOperationException("이 트리거는 시도 수를 세지 않습니다(관찰 트리거).");

        await using var connection = await TestTriggers.OpenAsync(_connectionString, cancellationToken);
        await using var command = new NpgsqlCommand($"SELECT CASE WHEN is_called THEN last_value ELSE 0 END FROM {sequence}", connection);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    /// <summary>P1 관찰 트리거가 기록한 값을 읽습니다.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>기록 순서의 관찰 값.</returns>
    public async Task<IReadOnlyList<IsolationObservation>> ReadIsolationObservationsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await TestTriggers.OpenAsync(_connectionString, cancellationToken);
        await using var command = new NpgsqlCommand(TestTriggerSql.SelectIsolationObservations, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var observations = new List<IsolationObservation>();
        while (await reader.ReadAsync(cancellationToken))
        {
            observations.Add(new IsolationObservation(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return observations;
    }

    /// <summary>모든 테스트 객체를 지웁니다(<see cref="TestTriggerSql.DropAll"/>, 여러 번 불러도 됨).</summary>
    /// <returns>정리 작업.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await TestTriggers.DropAllAsync(_connectionString, CancellationToken.None);
        _disposed = true;
    }
}
