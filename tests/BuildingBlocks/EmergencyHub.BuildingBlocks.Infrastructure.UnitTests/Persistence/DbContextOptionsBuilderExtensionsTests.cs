using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// database.md "공통 DbContext 등록"(S03-T02, BL-073): 재시도 설정(최대 횟수 · 최대 지연)은 공통 옵션 구성 메서드의 선택 인자다.
// 인자가 없으면 Npgsql 기본값(6회 · 30초), 있으면 그 값으로 EnableRetryOnFailure를 건다. 실행 전략 설정은 이 메서드 한 곳이다.
// 값은 ExecutionStrategySettings(리플렉션)로 읽는다(DB 없음).
[Trait("FR", "PRD-001/FR-06")]
public sealed class DbContextOptionsBuilderExtensionsTests
{
    private const int NpgsqlDefaultMaxRetryCount = 6;

    private static readonly TimeSpan NpgsqlDefaultMaxRetryDelay = TimeSpan.FromSeconds(30);

    [Fact]
    public void UseBuildingBlocksNpgsql_WithoutRetryOptions_UsesNpgsqlDefaultRetrySettings()
    {
        using var context = CreateContext(retry: null);

        var strategy = context.Database.CreateExecutionStrategy();

        strategy.RetriesOnFailure.Should().BeTrue();
        ExecutionStrategySettings.MaxRetryCount(strategy).Should().Be(NpgsqlDefaultMaxRetryCount);
        ExecutionStrategySettings.MaxRetryDelay(strategy).Should().Be(NpgsqlDefaultMaxRetryDelay);
    }

    [Fact]
    public void UseBuildingBlocksNpgsql_WithRetryOptions_UsesGivenRetrySettings()
    {
        using var context = CreateContext(new DbRetryOptions(3, TimeSpan.FromSeconds(5)));

        var strategy = context.Database.CreateExecutionStrategy();

        strategy.RetriesOnFailure.Should().BeTrue();
        ExecutionStrategySettings.MaxRetryCount(strategy).Should().Be(3);
        ExecutionStrategySettings.MaxRetryDelay(strategy).Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UseBuildingBlocksNpgsql_WithRetryOptions_KeepsSnakeCaseNamingConvention()
    {
        using var context = CreateContext(new DbRetryOptions(1, TimeSpan.FromSeconds(1)));

        context.Model.FindEntityType(typeof(Order))!.GetTableName().Should().Be("orders");
    }

    [Fact]
    public void UseBuildingBlocksNpgsql_ZeroRetryCountAndZeroDelay_AreAccepted()
    {
        // 엣지: 0회 · 0초는 "재시도하지 않음"이라는 유효한 설정이다(실행 전략 형식은 그대로).
        using var context = CreateContext(new DbRetryOptions(0, TimeSpan.Zero));

        var strategy = context.Database.CreateExecutionStrategy();

        ExecutionStrategySettings.MaxRetryCount(strategy).Should().Be(0);
        ExecutionStrategySettings.MaxRetryDelay(strategy).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void UseBuildingBlocksNpgsql_TypedBuilderWithRetryOptions_UsesGivenRetrySettings()
    {
        var options = new DbContextOptionsBuilder<SampleWriteDbContext>()
            .UseBuildingBlocksNpgsql(SampleDbContexts.DummyConnectionString, new DbRetryOptions(2, TimeSpan.FromSeconds(4)))
            .Options;
        using var context = new SampleWriteDbContext(options);

        ExecutionStrategySettings.MaxRetryCount(context.Database.CreateExecutionStrategy()).Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UseBuildingBlocksNpgsql_BlankConnectionStringWithRetryOptions_ThrowsArgumentException(string connectionString)
    {
        var act = () => new DbContextOptionsBuilder().UseBuildingBlocksNpgsql(connectionString, new DbRetryOptions(1, TimeSpan.FromSeconds(1)));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DbRetryOptions_NegativeRetryCount_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new DbRetryOptions(-1, TimeSpan.FromSeconds(1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxRetryCount");
    }

    [Fact]
    public void DbRetryOptions_NegativeDelay_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new DbRetryOptions(1, TimeSpan.FromTicks(-1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxRetryDelay");
    }

    [Fact]
    public void DbRetryOptions_SameValues_AreEqual()
    {
        new DbRetryOptions(3, TimeSpan.FromSeconds(5)).Should().Be(new DbRetryOptions(3, TimeSpan.FromSeconds(5)));
    }

    private static SampleWriteDbContext CreateContext(DbRetryOptions? retry) =>
        new(new DbContextOptionsBuilder<SampleWriteDbContext>()
            .UseBuildingBlocksNpgsql(SampleDbContexts.DummyConnectionString, retry)
            .Options);
}
