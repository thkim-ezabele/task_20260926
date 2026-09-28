using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.Extensions.DependencyInjection;
using NpgsqlTypes;

namespace EmergencyHub.Employee.IntegrationTests.QueryPlans;

// S05-T06 완료 조건 ④: Repository가 실제로 보낸 SQL(가로챈 명령 원문 · 매개변수)을 10,000건 + ANALYZE 데이터에서 EXPLAIN해 인덱스 사용을 확인한다.
// 기대 계획의 원본은 dba 쿼리 명세(목록 → ix_employees_joined_on_id, 이름 → ix_employees_name_joined_on_id, = ANY → ux_employees_normalized_email, Sort 없음).
// 인덱스 판정은 앞쪽 페이지(OFFSET 0)로 한다. 깊은 페이지는 계획을 판정하지 않고 결과 순서 · DB 실행 시간만 본다(S07-T03). 개수는 전체 스캔이 정상이다.
// 데이터는 EmployeeBulkSeeder(10,000건 fixture 시더, 마이그레이션 시드 아님)가 넣는다.
// SQL 원문과 계획은 테스트 출력에 남긴다(진행 기록 · tester 재확인용).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-07")]
public sealed class EmployeeQueryPlanTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // DB 실행 시간 상한(NFR-03 200ms는 HTTP 요청 전체 기준이고 tester가 잰다. 여기서는 DB 몫이 그 1/4 안인지만 본다).
    private const double MaxDatabaseMilliseconds = 50;

    // 출력 줄 길이 상한(= ANY 배열 값이 계획 줄에 모두 찍히므로 자른다).
    private const int MaxOutputLineLength = 200;

    private static readonly string[] IndexNodes = ["Index Scan using ", "Index Only Scan using ", "Bitmap Index Scan on "];

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
        ActualTotalMilliseconds(plan).Should().BeLessThan(MaxDatabaseMilliseconds);
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
        ActualTotalMilliseconds(plan).Should().BeLessThan(MaxDatabaseMilliseconds);
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-06")]
    public async Task ListExistingNormalizedEmailsAsync_ThousandValuesAmongTenThousandRows_UsesNormalizedEmailUniqueIndexWithOneArrayParameter()
    {
        // 500개는 있고(seed2 · seed4 ...) 500개는 없다. = ANY 배열 매개변수 1개로 유니크 인덱스를 탄다(IN 나열 · 매개변수 N개 아님).
        var capture = new CommandCaptureInterceptor();
        await using var services = await SeedAndCreateServicesAsync(writeInterceptor: capture);
        await using var scope = services.CreateAsyncScope();
        var requested = Enumerable.Range(1, 1000).Select(i => i % 2 == 0 ? $"seed{i}@example.com" : $"new{i}@example.com").ToArray();

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

        count.Should().Be(EmployeeBulkSeeder.Rows);
        var command = capture.Commands.Should().ContainSingle().Subject;
        command.Text.Should().NotContain("OVER");
        var plan = await ExplainOnReadConnectionAsync(command);
        plan[0].Should().StartWith("Aggregate");
        ActualTotalMilliseconds(plan).Should().BeLessThan(MaxDatabaseMilliseconds);
    }

    // S07-T03 ③: 깊은 페이지(끝 페이지 · 끝을 넘은 페이지 · 상한 skip)도 실행 시간이 NFR-03 200ms보다 한참 작다. 계획(인덱스 또는 Seq Scan + top-N Sort)은 출력에 남기고 판정하지 않는다.
    // skip 9,980 / take 20 = pageSize 20 끝 페이지, 9,900 / 100 = pageSize 100 끝 페이지, 10,000 / 20 = 끝을 넘은 첫 페이지, 9,999,900 / 100 = page 100,000 × pageSize 100(상한).
    [Theory]
    [InlineData(5_000, 20)]
    [InlineData(9_980, 20)]
    [InlineData(9_900, 100)]
    [InlineData(10_000, 20)]
    [InlineData(9_999_900, 100)]
    public async Task ListOrderedByJoinedOnAsync_DeepPageAmongTenThousandRows_ReturnsListOrderAndRunsWellUnderNfrThreshold(int skip, int take)
    {
        var capture = new CommandCaptureInterceptor();
        await using var services = await SeedAndCreateServicesAsync(readInterceptor: capture);
        await using var scope = services.CreateAsyncScope();

        var items = await scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().ListOrderedByJoinedOnAsync(skip, take, CancellationToken);

        items.Select(item => item.Id).Should().Equal(await EmployeeBulkSeeder.IdsInListOrderAsync(Database, skip, take, CancellationToken));
        items.Should().HaveCount(Math.Clamp(EmployeeBulkSeeder.Rows - skip, 0, take));
        var plan = await ExplainOnReadConnectionAsync(capture.Commands.Should().ContainSingle().Subject);
        ActualTotalMilliseconds(plan).Should().BeLessThan(MaxDatabaseMilliseconds);
    }

    // 시더 행 규칙 고정: 동명이인 5명 중 가장 빠른 사람은 ID가 가장 작은 사람(g = 42)이 아니라 g = 6042다(입사일 순 정렬을 ID 순서와 구별).
    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_SeededNamesakes_ReturnsEarliestJoinedNotSmallestId()
    {
        await using var services = await SeedAndCreateServicesAsync();
        await using var scope = services.CreateAsyncScope();

        var found = await scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().FindFirstByNameAsync(Name.Create(EmployeeBulkSeeder.NamesakeName).Value, CancellationToken);

        found.Should().NotBeNull();
        found!.Id.Should().Be(Guid.Parse("0190a000-0000-7000-8000-00000000179a"), "g = 6042 = 0x179a");
        found.JoinedOn.Should().Be(new DateOnly(2017, 6, 23));
        (await EmployeeBulkSeeder.FirstIdByNameAsync(Database, EmployeeBulkSeeder.NamesakeName, CancellationToken)).Should().Be(found.Id);
        (await EmployeeBulkSeeder.FirstIdByNameAsync(Database, EmployeeBulkSeeder.MissingName, CancellationToken)).Should().BeNull();
    }

    // 계획 첫 줄(최상위 노드)의 "actual time=시작..끝"에서 끝 값(ms)을 읽는다. 전송 · 프로젝션을 뺀 DB 실행 시간이다.
    private static double ActualTotalMilliseconds(IReadOnlyList<string> plan)
    {
        var match = System.Text.RegularExpressions.Regex.Match(plan[0], @"actual time=[0-9.]+\.\.([0-9.]+)");
        match.Success.Should().BeTrue("EXPLAIN ANALYZE 첫 줄에 actual time이 있다: " + plan[0]);
        var milliseconds = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        TestContext.Current.TestOutputHelper?.WriteLine(FormattableString.Invariant($"-- DB 실행 시간(최상위 노드): {milliseconds:0.000}ms"));
        return milliseconds;
    }

    private async Task<ServiceProvider> SeedAndCreateServicesAsync(CommandCaptureInterceptor? readInterceptor = null, CommandCaptureInterceptor? writeInterceptor = null)
    {
        await EmployeeBulkSeeder.SeedAsync(Database, CancellationToken);

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
