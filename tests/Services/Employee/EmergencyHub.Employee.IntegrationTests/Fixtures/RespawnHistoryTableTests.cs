using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T02 오케스트레이션 결정 A 확인(testing-strategy.md Q10 · Q11, BL-082 S6): 이력 테이블 snake_case 예외는 테이블 이름 "__EFMigrationsHistory"에만 있고,
// Respawn TablesToIgnore · to_regclass는 테이블 이름(대소문자 그대로, 따옴표 없는 이름 / 따옴표로 감싼 regclass 식)으로 동작한다.
// 잘못된 형태의 Respawner는 DeleteSql만 보고 ResetAsync하지 않는다(이력이 비워져 뒤 테스트가 깨짐).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-09")]
public sealed class RespawnHistoryTableTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string HistoryTable = "__EFMigrationsHistory";

    // ---- 성공 ----

    [Fact]
    public void RespawnDeleteSql_FixtureOptions_TruncatesOnlyEmployeesTable()
    {
        var deleteSql = Database.RespawnDeleteSql;

        deleteSql.Trim().Should().Be("""truncate table "public"."employees" cascade;""");
    }

    [Fact]
    public async Task HistoryTable_AfterMigration_HasSnakeCaseColumnsAndOneInitialCreateRow()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await using var services = Database.CreateServices();
        await using var scope = services.CreateAsyncScope();
        var migrationId = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Database.GetMigrations().Should().ContainSingle().Subject;

        var columns = await ReadStringsAsync(
            connection, $"SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{HistoryTable}' ORDER BY ordinal_position");
        var primaryKey = await connection.ScalarAsync<string>(
            """SELECT conname::text FROM pg_constraint WHERE conrelid = 'public."__EFMigrationsHistory"'::regclass AND contype = 'p'""", CancellationToken);
        var rows = await ReadStringsAsync(connection, """SELECT migration_id || '|' || product_version FROM public."__EFMigrationsHistory" """);

        columns.Should().Equal("migration_id", "product_version");
        primaryKey.Should().Be("pk___ef_migrations_history");
        var row = rows.Should().ContainSingle().Subject.Split('|');
        row[0].Should().Be(migrationId).And.EndWith("_InitialCreate");
        row[1].Should().StartWith("8.0.");
    }

    // ---- 실패 ----

    [Fact]
    public async Task ToRegclass_UnquotedHistoryName_FoldsToLowercaseAndFindsNothing()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var quoted = await connection.ScalarAsync<bool>("""SELECT to_regclass('public."__EFMigrationsHistory"') IS NOT NULL""", CancellationToken);
        var unquoted = await connection.ScalarAsync<bool>("SELECT to_regclass('public.__EFMigrationsHistory') IS NULL", CancellationToken);

        (quoted, unquoted).Should().Be((true, true));
    }

    [Theory]
    [InlineData("__efmigrationshistory")]
    [InlineData("\"__EFMigrationsHistory\"")]
    public async Task RespawnerCreateAsync_IgnoreNameNotExactTableName_DoesNotExcludeHistoryTable(string ignoredName)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var respawner = await Respawner.CreateAsync(connection, Options(ignoredName));

        respawner.DeleteSql.Should().Contain(HistoryTable, "TablesToIgnore는 테이블 이름을 대소문자 그대로 비교한다");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task RespawnerCreateAsync_ExactTableName_ExcludesHistoryTable()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var respawner = await Respawner.CreateAsync(connection, Options(HistoryTable));

        respawner.DeleteSql.Should().NotContain(HistoryTable).And.Contain("\"employees\"");
    }

    private static RespawnerOptions Options(string ignoredName) => new()
    {
        DbAdapter = DbAdapter.Postgres,
        SchemasToInclude = ["public"],
        TablesToIgnore = [new Table("public", ignoredName)],
        WithReseed = false,
    };

    private static async Task<IReadOnlyList<string>> ReadStringsAsync(NpgsqlConnection connection, string sql)
    {
        var values = new List<string>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
