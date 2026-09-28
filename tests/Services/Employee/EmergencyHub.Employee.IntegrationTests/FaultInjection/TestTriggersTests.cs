using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

// S03-T06 장애 주입 도우미 스모크(testing-strategy.md "장애 주입 · 트리거 규칙"): 문서 SQL 원문으로 만들고, 폐기 때 지우고, 잔여 검사가 0이다.
// "1회만 실패"는 시퀀스로 세므로 롤백과 무관하게 두 번째 시도가 통과한다. P1 · P2 · P6 판정은 tester 시나리오.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-05")]
public sealed class TestTriggersTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // S05-T04: 새 스키마 컬럼(PRD-002 FR-01). normalized_email은 DB가 강제하지 않으므로 Domain과 같은 ToLowerInvariant 값을 넣는다.
    private const string InsertSql =
        "INSERT INTO employees (id, name, email, normalized_email, phone_number, joined_on, employee_status, created_at, updated_at) "
        + "VALUES ($1, 'Trigger Test', $2, $3, '010-1234-5678', DATE '2020-03-02', 1, now(), now())";

    // ---- 성공 ----

    [Fact]
    public async Task CreateIsolationProbeAsync_Insert_RecordsServerTransactionSettingsAndCleansUp()
    {
        IReadOnlyList<IsolationObservation> observations;

        await using (var probe = await TestTriggers.CreateIsolationProbeAsync(Database, CancellationToken))
        {
            await InsertAsync();
            observations = await probe.ReadIsolationObservationsAsync(CancellationToken);
        }

        observations.Should().Equal(new IsolationObservation("read committed", "off", "read committed"));
        (await TestTriggers.CountLeftoversAsync(Database, CancellationToken)).Should().Be(TestObjectCounts.None);
    }

    // ---- 실패 ----

    [Fact]
    public async Task CreateAlwaysFailAsync_Insert_FailsWith40001UntilDisposed()
    {
        await using (var trigger = await TestTriggers.CreateAlwaysFailAsync(Database, CancellationToken))
        {
            var act = InsertAsync;

            (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.SerializationFailure);
            (await trigger.ReadAttemptsAsync(CancellationToken)).Should().Be(1);
        }

        await InsertAsync();
        (await TestTriggers.CountLeftoversAsync(Database, CancellationToken)).Should().Be(TestObjectCounts.None);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task CreateCommitFailureOnceAsync_TwoAutocommitInserts_FailsOnlyFirstCommitCountedBySequence()
    {
        await using var trigger = await TestTriggers.CreateCommitFailureOnceAsync(Database, CancellationToken);
        (await trigger.ReadAttemptsAsync(CancellationToken)).Should().Be(0);

        var first = InsertAsync;

        (await first.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.SerializationFailure);
        await InsertAsync();
        (await trigger.ReadAttemptsAsync(CancellationToken)).Should().Be(2);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_IsIdempotentAndLeavesNothing()
    {
        var trigger = await TestTriggers.CreateAlwaysFailAsync(Database, CancellationToken);

        await trigger.DisposeAsync();
        await trigger.DisposeAsync();

        (await TestTriggers.CountLeftoversAsync(Database, CancellationToken)).Should().Be(TestObjectCounts.None);
    }

    private async Task InsertAsync()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        var email = $"Trigger-{Guid.NewGuid():N}@Example.com";
        await connection.ExecuteSqlAsync(InsertSql, CancellationToken, Guid.NewGuid(), email, email.ToLowerInvariant());
    }
}
