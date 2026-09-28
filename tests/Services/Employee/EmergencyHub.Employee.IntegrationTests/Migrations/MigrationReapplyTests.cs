using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Migrations;

// BL-082 마이그레이션 재적용 멱등(testing-strategy.md Q12, ADR-0012): 운영 적용 경로(MigrationService 등록 + 실행 전략 안 MigrateAsync)를 다시 실행해도,
// idempotent SQL(--idempotent와 같은 IMigrator.GenerateScript)을 psql로 employee_app이 두 번 실행해도 오류 없이 이력 1행이다.
// 빈 DB 시나리오는 테이블을 지웠다가 스크립트로 다시 만들고, 실패해도 finally에서 운영 경로로 스키마를 복구한다(같은 컬렉션의 뒤 테스트 보호).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-09")]
[Trait("FR", "PRD-001/FR-08")]
public sealed class MigrationReapplyTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string HistoryCountSql = """SELECT count(*) FROM public."__EFMigrationsHistory" """;
    private const string HistoryExistsNotice = """relation "__EFMigrationsHistory" already exists, skipping""";

    // ---- 성공 ----

    [Fact]
    public async Task ApplyMigrationsAsync_RunTwiceMoreOnMigratedDatabase_KeepsOneHistoryRowAndExistingData()
    {
        await using var services = Database.CreateServices();
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken)).IsSuccess.Should().BeTrue();

        await Database.ApplyMigrationsAsync(CancellationToken);
        await Database.ApplyMigrationsAsync(CancellationToken);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        (await db.Database.GetAppliedMigrationsAsync(CancellationToken)).Should().Equal(db.Database.GetMigrations());
        (await db.Database.GetPendingMigrationsAsync(CancellationToken)).Should().BeEmpty();
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>(HistoryCountSql, CancellationToken)).Should().Be(1);
        (await connection.ScalarAsync<long>("SELECT count(*) FROM employees", CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task IdempotentScript_RunTwiceWithPsqlOnMigratedDatabase_SucceedsWithoutErrors()
    {
        var script = await GenerateIdempotentScriptAsync();

        var first = await Database.ExecutePsqlScriptAsync(script, CancellationToken);
        var second = await Database.ExecutePsqlScriptAsync(script, CancellationToken);

        foreach (var run in new[] { first, second })
        {
            run.ExitCode.Should().Be(0, run.Stderr);
            run.Stderr.Should().NotContain("ERROR").And.Contain(HistoryExistsNotice);
        }

        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>(HistoryCountSql, CancellationToken)).Should().Be(1);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task IdempotentScript_RunTwiceWithPsqlOnEmptyDatabaseThenMigrateAsync_CreatesSchemaOnceOwnedByAppRole()
    {
        var script = await GenerateIdempotentScriptAsync();
        try
        {
            await using (var connection = await Database.OpenWriteConnectionAsync(CancellationToken))
            {
                await connection.ExecuteSqlAsync("""DROP TABLE employees; DROP TABLE public."__EFMigrationsHistory";""", CancellationToken);
            }

            var first = await Database.ExecutePsqlScriptAsync(script, CancellationToken);
            var second = await Database.ExecutePsqlScriptAsync(script, CancellationToken);
            await Database.ApplyMigrationsAsync(CancellationToken);

            first.ExitCode.Should().Be(0, first.Stderr);
            first.Stderr.Should().NotContain("ERROR").And.NotContain(HistoryExistsNotice);
            second.ExitCode.Should().Be(0, second.Stderr);
            second.Stderr.Should().NotContain("ERROR");
            second.Stderr.Split(HistoryExistsNotice).Length.Should().Be(2, "두 번째 실행의 NOTICE는 이력 테이블 한 번뿐이다(오류 아님)");
            await using var verify = await Database.OpenWriteConnectionAsync(CancellationToken);
            (await verify.ScalarAsync<long>(HistoryCountSql, CancellationToken)).Should().Be(1);
            (await verify.ScalarAsync<string>("SELECT tableowner::text FROM pg_tables WHERE schemaname = 'public' AND tablename = 'employees'", CancellationToken))
                .Should().Be(EmployeeDatabaseSettings.AppRoleName);
            (await verify.ScalarAsync<long>(
                "SELECT count(*) FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'employees' AND indexname = $1", CancellationToken, EmployeeDbNames.EmailUniqueIndex.Value))
                .Should().Be(1);
        }
        finally
        {
            await RestoreSchemaAsync();
        }
    }

    private async Task<string> GenerateIdempotentScriptAsync()
    {
        await using var services = Database.CreateServices();
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        return db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
    }

    // 스키마가 부분적으로 남았으면 둘 다 지우고 운영 경로로 다시 적용한다.
    private async Task RestoreSchemaAsync()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken.None);
        var intact = await connection.ScalarAsync<bool>(
            """SELECT to_regclass('public.employees') IS NOT NULL AND to_regclass('public."__EFMigrationsHistory"') IS NOT NULL""", CancellationToken.None);
        if (!intact)
        {
            await connection.ExecuteSqlAsync("""DROP TABLE IF EXISTS employees; DROP TABLE IF EXISTS public."__EFMigrationsHistory";""", CancellationToken.None);
            await Database.ApplyMigrationsAsync(CancellationToken.None);
        }

        NpgsqlConnection.ClearAllPools();
    }
}
