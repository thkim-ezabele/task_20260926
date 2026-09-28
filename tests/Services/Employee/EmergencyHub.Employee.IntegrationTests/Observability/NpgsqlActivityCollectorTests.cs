using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Observability;

// S06-T06 도구(developer): Npgsql span 수집기. BL-024 본 테스트(Api 요청의 span 태그에 비밀번호 · 입력 값 없음)는 tester가 쓴다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/NFR-04")]
public sealed class NpgsqlActivityCollectorTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string Probe = "SELECT 'npgsql-activity-probe'";

    // ---- 성공 ----

    [Fact]
    public async Task Activities_CommandExecuted_CapturesSpanWithStatementTag()
    {
        using var collector = new NpgsqlActivityCollector();

        await ExecuteProbeAsync();

        var span = collector.Activities.Should().ContainSingle(activity => activity.Tag("db.statement") == Probe).Which;
        span.SourceName.Should().Be(NpgsqlActivityCollector.SourceName);
        span.Tags.Select(tag => tag.Key).Should().Contain("db.system");
        span.Dump().Should().Contain(Probe);
    }

    // ---- 실패 ----

    [Fact]
    public async Task Activities_AfterDispose_CollectsNothingMore()
    {
        var collector = new NpgsqlActivityCollector();
        collector.Dispose();

        await ExecuteProbeAsync();

        collector.Activities.Should().BeEmpty();
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Activities_FilterRejects_SkipsSpanButClearResetsList()
    {
        using var all = new NpgsqlActivityCollector();
        using var none = new NpgsqlActivityCollector(_ => false);

        await ExecuteProbeAsync();

        none.Activities.Should().BeEmpty("필터가 거부한 span은 모으지 않는다");
        all.Activities.Should().NotBeEmpty("리스너 여러 개가 같은 span을 각자 받는다");
        all.Clear();
        all.Activities.Should().BeEmpty();
    }

    private async Task ExecuteProbeAsync()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await using var command = new NpgsqlCommand(Probe, connection);
        await command.ExecuteScalarAsync(CancellationToken);
    }
}
