using System.Net;
using System.Text.Json;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;

namespace EmergencyHub.Employee.IntegrationTests.Performance;

// S07-T03 완료 조건 ③(PRD-002 NFR-03 "10,000건에서 목록 · 이름 조회 200ms 이내, 측정 방식 NFR-02와 같음"):
// 10,000건 fixture 시더(마이그레이션 시드 아님)로 한 번 넣은 뒤 실제 Api 파이프라인(TestServer) + 컨테이너 DB에서 워밍업 1회 뒤 5회 재고 중앙값을 단언한다.
// 회차마다 DB를 비우지 않는다(시드 보존). 로컬 목표는 200ms, 단언은 CI 임계값 400ms(NFR-02와 같이 2배)이며 측정값은 테스트 출력에 남긴다(진행 기록 원본).
// 대상: 첫 쪽(page=1&pageSize=20), 깊은 쪽(page=500&pageSize=20 = OFFSET 9,980, 마지막 쪽), 동명이인 5명 이름 조회(퍼센트 인코딩). 측정 조건: EnableSensitiveDataLogging 끔.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("NFR", "PRD-002/NFR-03")]
[Trait("FR", "PRD-002/FR-07")]
[Trait("FR", "PRD-002/FR-08")]
public sealed class EmployeeQueryPerformanceTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const int Iterations = 5;
    private const int PageSize = 20;
    private static readonly TimeSpan CiThreshold = TimeSpan.FromMilliseconds(400);
    private static readonly Guid NamesakeExpectedId = Guid.Parse("0190a000-0000-7000-8000-00000000179a");

    // ---- 성공: 목록 첫 쪽 · 깊은 쪽 ----

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public async Task GetList_10000Rows_MedianWithinCiThresholdAndReturnsExpectedPage(int page)
    {
        await EmployeeBulkSeeder.SeedAsync(Database, CancellationToken);
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        DbContextOptionsInspection.IsSensitiveDataLoggingEnabled(factory.Services).Should().BeFalse("측정 조건: EnableSensitiveDataLogging 끔(NFR-04)");
        var uri = FormattableString.Invariant($"/api/employee?page={page}&pageSize={PageSize}");
        var expected = await EmployeeBulkSeeder.IdsInListOrderAsync(Database, (page - 1) * PageSize, PageSize, CancellationToken);
        var bodies = new List<string>();

        var result = await PerformanceMeasurement.MeasureAsync(
            warmupCount: 1,
            iterationCount: Iterations,
            action: async cancellationToken =>
            {
                using var response = await client.GetAsync(uri, cancellationToken);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                bodies.Add(await response.Content.ReadAsStringAsync(cancellationToken));
            },
            CancellationToken);

        TestContext.Current.TestOutputHelper!.WriteLine($"NFR-03 목록 {uri}(10,000건, TestServer + Testcontainers): {result.Describe()}");
        bodies.Should().HaveCount(1 + Iterations).And.OnlyContain(body => body == bodies[0], "회차마다 같은 응답(시드 보존)");
        using var json = JsonDocument.Parse(bodies[0]);
        json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(EmployeeBulkSeeder.Rows);
        json.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(expected);
        result.Median.Should().BeLessThan(CiThreshold, "CI 임계값은 NFR-03 200ms의 2배");
    }

    // ---- 성공 · 엣지: 동명이인 5명 중 입사일이 가장 빠른 1명(ID가 가장 작은 사람이 아님) ----

    [Fact]
    public async Task GetByName_NamesakeOfFiveIn10000Rows_MedianWithinCiThresholdAndReturnsEarliestJoined()
    {
        await EmployeeBulkSeeder.SeedAsync(Database, CancellationToken);
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        DbContextOptionsInspection.IsSensitiveDataLoggingEnabled(factory.Services).Should().BeFalse("측정 조건: EnableSensitiveDataLogging 끔(NFR-04)");
        var uri = "/api/employee/" + Uri.EscapeDataString(EmployeeBulkSeeder.NamesakeName);
        (await EmployeeBulkSeeder.FirstIdByNameAsync(Database, EmployeeBulkSeeder.NamesakeName, CancellationToken)).Should().Be(NamesakeExpectedId, "SQL 기대값과 상수가 같다");
        var bodies = new List<string>();

        var result = await PerformanceMeasurement.MeasureAsync(
            warmupCount: 1,
            iterationCount: Iterations,
            action: async cancellationToken =>
            {
                using var response = await client.GetAsync(uri, cancellationToken);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                bodies.Add(await response.Content.ReadAsStringAsync(cancellationToken));
            },
            CancellationToken);

        TestContext.Current.TestOutputHelper!.WriteLine($"NFR-03 이름 조회 {uri}(10,000건, 동명이인 5명): {result.Describe()}");
        bodies.Should().HaveCount(1 + Iterations).And.OnlyContain(body => body == bodies[0]);
        using var json = JsonDocument.Parse(bodies[0]);
        json.RootElement.GetProperty("id").GetGuid().Should().Be(NamesakeExpectedId);
        json.RootElement.GetProperty("joined").GetString().Should().Be("2017-06-23");
        result.Median.Should().BeLessThan(CiThreshold, "CI 임계값은 NFR-03 200ms의 2배");
    }
}
