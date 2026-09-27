using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04: 읽기 전용 DbContext 기반. 추적 기본값 NoTracking, SaveChanges 오버로드 4개 모두 InvalidOperationException(database.md "읽기 / 쓰기 연결 분리").
// 읽기 연결(default_transaction_read_only)의 DB 거부는 S03-T05.
[Trait("FR", "PRD-001/FR-06")]
public sealed class ReadDbContextBaseTests : IDisposable
{
    private readonly SampleReadDbContext _context = SampleDbContexts.CreateRead();

    public void Dispose() => _context.Dispose();

    // ---- 성공 ----

    [Fact]
    public void QueryTrackingBehavior_OfReadContext_IsNoTracking()
    {
        _context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
    }

    [Fact]
    public void QueryTrackingBehavior_OfWriteContext_StaysTrackAll()
    {
        using var write = SampleDbContexts.CreateWrite();

        write.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.TrackAll);
    }

    // ---- 실패: 저장 차단 ----

    [Fact]
    public void SaveChanges_NoArguments_Throws()
    {
        var act = () => _context.SaveChanges();

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(SampleReadDbContext)}*");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SaveChanges_AcceptAllChangesFlag_Throws(bool acceptAllChangesOnSuccess)
    {
        var act = () => _context.SaveChanges(acceptAllChangesOnSuccess);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveChangesAsync_CancellationToken_Throws()
    {
        var act = () => _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveChangesAsync_AcceptAllChangesFlag_Throws(bool acceptAllChangesOnSuccess)
    {
        var act = () => _context.SaveChangesAsync(acceptAllChangesOnSuccess, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void SaveChanges_WithAddedEntity_ThrowsAndKeepsEntityAdded()
    {
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);

        var act = () => _context.SaveChanges();

        act.Should().Throw<InvalidOperationException>();
        _context.Entry(order).State.Should().Be(EntityState.Added, "저장 경로에 들어가지 않으므로 상태가 바뀌지 않는다");
    }

    [Fact]
    public async Task SaveChangesAsync_CanceledToken_StillThrowsInvalidOperation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => _context.SaveChangesAsync(cancellation.Token);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void SaveChangesOverloads_AreSealedSoDerivedContextsCannotReopenSaving()
    {
        var overloads = typeof(ReadDbContextBase).GetMethods()
            .Where(method => method.Name is nameof(DbContext.SaveChanges) or nameof(DbContext.SaveChangesAsync))
            .ToList();

        overloads.Should().HaveCount(4);
        overloads.Should().OnlyContain(method => method.DeclaringType == typeof(ReadDbContextBase) && method.IsFinal);
    }
}
