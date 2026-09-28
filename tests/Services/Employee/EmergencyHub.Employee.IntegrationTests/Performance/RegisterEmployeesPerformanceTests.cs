using System.Globalization;
using System.Net;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.Http;
using EmergencyHub.Employee.IntegrationTests.TestData;

namespace EmergencyHub.Employee.IntegrationTests.Performance;

// S06-T06 완료 조건 ⑥(PRD-002 NFR-02 "1,000행 CSV 등록 2초 이내, CI는 느슨한 임계값(2배)", "EF 배치 크기를 측정 · 기록"):
// 실제 Api 파이프라인(TestServer) + 컨테이너 DB에서 raw text/csv 1,000행 POST를 워밍업 1회 뒤 5회 재고 중앙값을 4초(2초 × 2)로 단언한다.
// 회차마다 DB를 비우고(측정 제외) 같은 본문을 보낸다. 측정 조건: EnableSensitiveDataLogging 끔. 결과 · 명령 분할 수는 테스트 출력에 남긴다(진행 기록 원본).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("NFR", "PRD-002/NFR-02")]
public sealed class RegisterEmployeesPerformanceTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const int Rows = 1000;
    private const int Iterations = 5;
    private static readonly TimeSpan CiThreshold = TimeSpan.FromSeconds(4);

    // ---- 성공: 1,000행 CSV 중앙값 4초 이내, 요청당 INSERT 명령 수 기록 ----

    [Fact]
    public async Task Post_1000RowCsv_MedianWithinCiThresholdAndBatchSizeRecorded()
    {
        var counter = new CommandCountingInterceptor();
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteInterceptors = [counter] });
        using var client = factory.CreateClient();
        DbContextOptionsInspection.IsSensitiveDataLoggingEnabled(factory.Services).Should().BeFalse("측정 조건: EnableSensitiveDataLogging 끔(NFR-04)");
        var csv = EmployeeImportData.Csv(Rows);
        var perRequest = new List<(int Commands, int InsertCommands, int InsertStatements, int Parameters)>();

        var result = await PerformanceMeasurement.MeasureAsync(
            warmupCount: 1,
            iterationCount: Iterations,
            prepare: async cancellationToken =>
            {
                await Database.ResetAsync(cancellationToken);
                counter.Clear();
            },
            action: async cancellationToken =>
            {
                using var content = ImportContent.Raw(csv, "text/csv");
                using var response = await client.PostAsync(ImportContent.RegisterUri, content, cancellationToken);
                response.StatusCode.Should().Be(HttpStatusCode.Created);
                var inserts = counter.InsertCommands;
                perRequest.Add((counter.Commands.Count, inserts.Count, inserts.Sum(command => command.InsertStatementCount), inserts.Sum(command => command.ParameterCount)));
            },
            CancellationToken);

        var output = TestContext.Current.TestOutputHelper!;
        output.WriteLine($"NFR-02 1,000행 CSV(raw text/csv, TestServer + Testcontainers): {result.Describe()}");
        foreach (var (commands, insertCommands, insertStatements, parameters) in perRequest)
        {
            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"요청당 DB 명령 {commands}개, INSERT 명령(배치) {insertCommands}개, INSERT 문 {insertStatements}개, INSERT 매개변수 {parameters}개"));
        }

        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(Rows, "마지막 회차의 1,000행이 저장돼 있다");
        perRequest.Should().HaveCount(1 + Iterations).And.AllSatisfy(request => request.InsertStatements.Should().Be(Rows, "행마다 INSERT 문 1개"));
        perRequest.Select(request => request.InsertCommands).Distinct().Should().ContainSingle("회차마다 명령 분할 수가 같다");
        result.Median.Should().BeLessThan(CiThreshold, "CI 임계값은 NFR-02 2초의 2배");
    }

    // ---- 실패 · 엣지: 1,000행 경계 — 1,001행은 파싱 단계에서 거부되어 INSERT 명령이 하나도 나가지 않는다 ----

    [Fact]
    public async Task Post_1001RowCsv_RejectedBeforeAnyDbCommand()
    {
        var counter = new CommandCountingInterceptor();
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteInterceptors = [counter] });
        using var client = factory.CreateClient();
        counter.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(Rows + 1), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        counter.Commands.Should().BeEmpty("21027은 Handler 파싱 단계에서 멈춰 사전 조회 · 저장 명령이 없다");
    }
}
