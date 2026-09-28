using EmergencyHub.Employee.IntegrationTests.TestData;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T06 fixture 스모크(testing-strategy.md "테스트 DB 구성 (fixture)"): 초기화 스크립트 공유 마운트 → employee_app(슈퍼유저 아님) · DB 소유자,
// 읽기 연결 거부(25006), 실행 전략 안 MigrateAsync 적용 이력 1행, Respawn이 이력 테이블을 남기고 데이터만 비움, 운영 등록으로 커밋.
// P1~P8 · S 시나리오 전체는 tester 범위다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-09")]
public sealed class EmployeeDatabaseFixtureTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string HistoryCountSql = """SELECT count(*) FROM public."__EFMigrationsHistory" """;
    private const string EmployeeCountSql = "SELECT count(*) FROM employees";

    // ---- 성공 ----

    [Fact]
    public async Task WriteConnection_AfterInitialize_ConnectsAsEmployeeAppWithoutSuperuser()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var currentUser = await connection.ScalarAsync<string>("SELECT current_user", CancellationToken);
        var isSuperuser = await connection.ScalarAsync<bool>("SELECT rolsuper FROM pg_roles WHERE rolname = current_user", CancellationToken);
        var databaseOwner = await connection.ScalarAsync<string>(
            "SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname = current_database()", CancellationToken);

        currentUser.Should().Be(EmployeeDatabaseSettings.AppRoleName);
        isSuperuser.Should().BeFalse();
        databaseOwner.Should().Be(EmployeeDatabaseSettings.AppRoleName);
        connection.Database.Should().Be(EmployeeDatabaseSettings.DatabaseName);
    }

    [Fact]
    public async Task ApplyMigrations_AtInitialize_LeavesOneHistoryRowInPublicSchema()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var quotedExists = await connection.ScalarAsync<bool>("""SELECT to_regclass('public."__EFMigrationsHistory"') IS NOT NULL""", CancellationToken);
        var historyRows = await connection.ScalarAsync<long>(HistoryCountSql, CancellationToken);

        quotedExists.Should().BeTrue();
        historyRows.Should().Be(1);
    }

    [Fact]
    public async Task CreateServices_AddAndCommit_StoresRowThroughProductionRegistration()
    {
        await using var services = Database.CreateServices();

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>(EmployeeCountSql, CancellationToken)).Should().Be(1);
    }

    // ---- 실패 ----

    [Fact]
    public async Task ReadConnection_Insert_IsRejectedWithReadOnlyTransaction25006()
    {
        await using var connection = await Database.OpenReadConnectionAsync(CancellationToken);

        var act = () => connection.ExecuteSqlAsync(
            "INSERT INTO employees (id, name, email, normalized_email, phone_number, joined_on, employee_status, created_at, updated_at) "
            + "VALUES ($1, 'x', 'x@example.com', 'x@example.com', '010-1234-5678', DATE '2020-03-02', 1, now(), now())",
            CancellationToken,
            Guid.NewGuid());

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.ReadOnlySqlTransaction);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task ResetAsync_AfterRowsWereAdded_EmptiesTablesButKeepsMigrationHistory()
    {
        await using (var services = Database.CreateServices())
        {
            (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        }

        await Database.ResetAsync(CancellationToken);

        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>(EmployeeCountSql, CancellationToken)).Should().Be(0);
        (await connection.ScalarAsync<long>(HistoryCountSql, CancellationToken)).Should().Be(1);
        Database.RespawnDeleteSql.Should().Contain("\"employees\"").And.NotContain("__EFMigrationsHistory");
    }

    [Fact]
    public void ConnectionStringSettings_ReadDiffersFromWriteOnlyByReadOnlyOptionAndHasNoForbiddenOptions()
    {
        var write = new NpgsqlConnectionStringBuilder(Database.ConnectionStringSettings[EmployeeDatabaseFixture.WriteConnectionKey]);
        var read = new NpgsqlConnectionStringBuilder(Database.ConnectionStringSettings[EmployeeDatabaseFixture.ReadConnectionKey]);

        Database.ConnectionStringSettings.Keys.Should().BeEquivalentTo(EmployeeDatabaseFixture.WriteConnectionKey, EmployeeDatabaseFixture.ReadConnectionKey);
        write.Options.Should().BeNullOrEmpty();
        read.Options.Should().Be(EmployeeDatabaseFixture.ReadOnlyConnectionOptions);
        (read.Host, read.Port, read.Database, read.Username).Should().Be((write.Host, write.Port, write.Database, write.Username));
        write.IncludeErrorDetail.Should().BeFalse();
        write.PersistSecurityInfo.Should().BeFalse();
        read.IncludeErrorDetail.Should().BeFalse();
        read.PersistSecurityInfo.Should().BeFalse();
    }
}
