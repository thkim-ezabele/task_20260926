namespace EmergencyHub.Employee.IntegrationTests.Performance;

// S06-T06 도구(developer): 워밍업 · N회 중앙값 측정 도우미. 1,000행 CSV 4초 단언 본 테스트는 tester가 쓴다.
public sealed class PerformanceMeasurementTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // ---- 성공 ----

    [Fact]
    public async Task MeasureAsync_WarmupAndIterations_RunsPrepareBeforeEachActionAndTimesOnlyAction()
    {
        var calls = new List<string>();

        var result = await PerformanceMeasurement.MeasureAsync(
            warmupCount: 1,
            iterationCount: 3,
            prepare: async token =>
            {
                calls.Add("prepare");
                await Task.Delay(200, token);
            },
            action: _ =>
            {
                calls.Add("action");
                return Task.CompletedTask;
            },
            CancellationToken);

        calls.Should().Equal(Enumerable.Repeat<string[]>(["prepare", "action"], 4).SelectMany(pair => pair));
        result.Warmups.Should().HaveCount(1);
        result.Samples.Should().HaveCount(3);
        result.Max.Should().BeLessThan(TimeSpan.FromMilliseconds(200), "준비 작업(200ms 대기)은 재지 않는다");
    }

    [Fact]
    public void Median_OddAndEvenCounts_ReturnsMiddleOrAverageOfMiddleTwo()
    {
        var odd = new MeasurementResult([], [Ms(30), Ms(10), Ms(20)]);
        var even = new MeasurementResult([Ms(999)], [Ms(40), Ms(10), Ms(30), Ms(20)]);

        odd.Median.Should().Be(Ms(20));
        even.Median.Should().Be(Ms(25), "워밍업은 중앙값에 넣지 않는다");
        even.Describe().Should().Be("median=25.0ms samples=[40.0, 10.0, 30.0, 20.0] warmups=[999.0]");
    }

    // ---- 실패 ----

    [Fact]
    public async Task MeasureAsync_ZeroIterations_Throws()
    {
        var act = () => PerformanceMeasurement.MeasureAsync(0, 0, _ => Task.CompletedTask, _ => Task.CompletedTask, CancellationToken);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void Median_NoSamples_Throws()
    {
        var act = () => new MeasurementResult([], []).Median;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void InsertStatementCount_BatchedCommandText_CountsEachInsert()
    {
        var batch = new CountedCommand("Reader", "INSERT INTO employees (id) VALUES (@p0);\nINSERT INTO employees (id) VALUES (@p1);", 2);
        var select = new CountedCommand("Reader", "SELECT 1", 0);

        batch.InsertStatementCount.Should().Be(2);
        select.InsertStatementCount.Should().Be(0);
    }

    private static TimeSpan Ms(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
}
