using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NpgsqlTypes;

namespace EmergencyHub.Employee.IntegrationTests.QueryPlans;

// S05-T06 완료 조건 ④: Repository가 실제로 보낸 SQL(가로챈 명령 원문 · 매개변수)을 10,000건 + ANALYZE 데이터에서 EXPLAIN해 인덱스 사용을 확인한다.
// 기대 계획의 원본은 dba 쿼리 명세(목록 → ix_employees_joined_on_id, 이름 → ix_employees_name_joined_on_id, = ANY → ux_employees_normalized_email, Sort 없음).
// 판정은 앞쪽 페이지(OFFSET 0)로 한다. 끝 페이지는 Seq Scan + Sort가 될 수 있고 정상이다(dba 실측). 개수는 전체 스캔이 정상이다.
// SQL 원문과 계획은 테스트 출력에 남긴다(진행 기록 · tester 재확인용).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-07")]
public sealed class EmployeeQueryPlanTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const int SeedRows = 10_000;

    // 출력 줄 길이 상한(= ANY 배열 값이 계획 줄에 모두 찍히므로 자른다).
    private const int MaxOutputLineLength = 200;

    private static readonly string[] IndexNodes = ["Index Scan using ", "Index Only Scan using ", "Bitmap Index Scan on "];

    // 이름 2,000종(이름당 5명, 동명이인), 입사일 1990 ~ 2025 분포, 10%는 비활성. normalized_email = ToLowerInvariant(email)(DB가 강제하지 않음).
    private const string SeedSql =
        """
        SELECT setseed(0.42);
        INSERT INTO employees (id, name, email, normalized_email, phone_number, joined_on, employee_status, created_at, updated_at)
        SELECT gen_random_uuid(), '직원' || (g % 2000), 'User' || g || '@Example.com', 'user' || g || '@example.com',
               '010-' || lpad((g % 10000)::text, 4, '0') || '-' || lpad((g % 7919)::text, 4, '0'),
               date '1990-01-01' + (random() * 13000)::int, CASE WHEN g % 10 = 0 THEN 2 ELSE 1 END, now(), now()
        FROM generate_series(1, 10000) AS g;
        ANALYZE employees;
        """;

    // ---- 성공 ----

    [Fact]
    public async Task ListOrderedByJoinedOnAsync_FirstPageAmongTenThousandRows_UsesJoinedOnIdIndexWithoutSort()
    {
        var capture = new CommandCaptureInterceptor();
        await using var services = await SeedAndCreateServicesAsync(readInterceptor: capture);
        await using var scope = services.CreateAsyncScope();

        var items = await scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().ListOrderedByJoinedOnAsync(0, 20, CancellationToken);

        items.Should().HaveCount(20);
        var plan = await ExplainOnReadConnectionAsync(capture.Commands.Should().ContainSingle().Subject);
        plan.Should().Contain(line => line.Contains("Index Scan using ix_employees_joined_on_id", StringComparison.Ordinal));
        plan.Should().NotContain(line => line.Contains("Sort", StringComparison.Ordinal) || line.Contains("Seq Scan", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_NamesakesAmongTenThousandRows_UsesNameJoinedOnIdIndexWithoutSort()
    {
        var capture = new CommandCaptureInterceptor();
        await using var services = await SeedAndCreateServicesAsync(readInterceptor: capture);
        await using var scope = services.CreateAsyncScope();

        var found = await scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().FindFirstByNameAsync(Name.Create("직원42").Value, CancellationToken);

        found.Should().NotBeNull();
        var plan = await ExplainOnReadConnectionAsync(capture.Commands.Should().ContainSingle().Subject);
        plan.Should().Contain(line => line.Contains("Index Scan using ix_employees_name_joined_on_id", StringComparison.Ordinal));
        plan.Should().Contain(line => line.Contains("Index Cond: ((name)::text = ", StringComparison.Ordinal));
        plan.Should().NotContain(line => line.Contains("Sort", StringComparison.Ordinal) || line.Contains("Seq Scan", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-06")]
    public async Task ListExistingNormalizedEmailsAsync_ThousandValuesAmongTenThousandRows_UsesNormalizedEmailUniqueIndexWithOneArrayParameter()
    {
        // 500개는 있고(user2 · user4 ...) 500개는 없다. = ANY 배열 매개변수 1개로 유니크 인덱스를 탄다(IN 나열 · 매개변수 N개 아님).
        var capture = new CommandCaptureInterceptor();
        await using var services = await SeedAndCreateServicesAsync(writeInterceptor: capture);
        await using var scope = services.CreateAsyncScope();
        var requested = Enumerable.Range(1, 1000).Select(i => i % 2 == 0 ? $"user{i}@example.com" : $"new{i}@example.com").ToArray();

        var existing = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().ListExistingNormalizedEmailsAsync(requested, CancellationToken);

        existing.Should().HaveCount(500);
        var command = capture.Commands.Should().ContainSingle().Subject;
        command.Text.Should().Contain("= ANY (@");
        command.Parameters.Should().ContainSingle();
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        var plan = await ExplainAndLogAsync(connection, command);
        // ANALYZE만 한 직후(가시성 맵 없음)는 Bitmap Index Scan, VACUUM 뒤에는 Index Only Scan이 될 수 있다. 어느 쪽이든 유니크 인덱스를 쓰고 Seq Scan이 없어야 한다.
        plan.Should().Contain(line => IndexNodes.Any(node => line.Contains(node + EmployeeDbNames.NormalizedEmailUniqueIndex.Value, StringComparison.Ordinal)));
        plan.Should().NotContain(line => line.Contains("Seq Scan", StringComparison.Ordinal));
    }

    // ---- 엣지 ----

    [Fact]
    public async Task CountAsync_TenThousandRows_SendsPlainCountWithoutWindowFunction()
    {
        // 개수는 목록과 별도 문장이고 COUNT(*) OVER()가 아니다. 전체 개수라 전체 스캔(Seq Scan + Aggregate)이 정상이다.
        var capture = new CommandCaptureInterceptor();
        await using var services = await SeedAndCreateServicesAsync(readInterceptor: capture);
        await using var scope = services.CreateAsyncScope();

        var count = await scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().CountAsync(CancellationToken);

        count.Should().Be(SeedRows);
        var command = capture.Commands.Should().ContainSingle().Subject;
        command.Text.Should().NotContain("OVER");
        var plan = await ExplainOnReadConnectionAsync(command);
        plan[0].Should().StartWith("Aggregate");
    }

    private async Task<ServiceProvider> SeedAndCreateServicesAsync(CommandCaptureInterceptor? readInterceptor = null, CommandCaptureInterceptor? writeInterceptor = null)
    {
        await using (var connection = await Database.OpenWriteConnectionAsync(CancellationToken))
        {
            await connection.ExecuteSqlAsync(SeedSql, CancellationToken);
            (await connection.ScalarAsync<long>("SELECT count(*) FROM employees", CancellationToken)).Should().Be(SeedRows);
        }

        return Database.CreateServices(new EmployeeServicesOptions
        {
            ReadInterceptors = readInterceptor is null ? [] : [readInterceptor],
            WriteInterceptors = writeInterceptor is null ? [] : [writeInterceptor],
        });
    }

    private async Task<IReadOnlyList<string>> ExplainOnReadConnectionAsync(CapturedCommand command)
    {
        await using var connection = await Database.OpenReadConnectionAsync(CancellationToken);
        return await ExplainAndLogAsync(connection, command);
    }

    private static async Task<IReadOnlyList<string>> ExplainAndLogAsync(Npgsql.NpgsqlConnection connection, CapturedCommand command)
    {
        var plan = await QueryPlan.ExplainAsync(connection, command, CancellationToken);
        var output = TestContext.Current.TestOutputHelper;
        output?.WriteLine("-- SQL (EF Core 생성 원문)");
        output?.WriteLine(command.Text);
        output?.WriteLine("-- 매개변수: " + string.Join(", ", command.Parameters.Select(parameter => $"{parameter.ParameterName} {TypeName(parameter.NpgsqlDbType)}")));
        output?.WriteLine("-- 계획");
        foreach (var line in plan)
        {
            output?.WriteLine(line.Length <= MaxOutputLineLength ? line : string.Concat(line.AsSpan(0, MaxOutputLineLength), " ..."));
        }

        return plan;
    }

    private static string TypeName(NpgsqlDbType type) =>
        type.HasFlag(NpgsqlDbType.Array) ? $"{type & ~NpgsqlDbType.Array}[]" : type.ToString();
}
