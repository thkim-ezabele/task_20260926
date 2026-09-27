using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Auditing;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04: 감사 shadow property(created_at · updated_at)를 채우는 규칙(database.md "감사 컬럼" 상태별 표).
// 판정 로직(ApplyAuditValues)은 ChangeTracker와 now만 받으므로 DB 없이 확인한다. 인터셉터 경로는 저장을 건너뛰는 대역 인터셉터로 확인한다.
// 실제 DB 저장 동작은 S03-T05.
[Trait("FR", "PRD-001/FR-06")]
public sealed class AuditSaveChangesInterceptorTests : IDisposable
{
    private static readonly DateTimeOffset Past = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 3, 4, 5, TimeSpan.Zero);

    private readonly SampleWriteDbContext _context = SampleDbContexts.CreateWrite();

    public void Dispose() => _context.Dispose();

    // ---- 성공: 상태별 ----

    [Fact]
    public void Apply_AddedRootEntity_SetsCreatedAtAndUpdatedAtToSameNow()
    {
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        var entry = _context.Entry(order);
        CreatedAt(entry).CurrentValue.Should().Be(Now);
        UpdatedAt(entry).CurrentValue.Should().Be(Now);
    }

    [Fact]
    public void Apply_ModifiedRootEntity_SetsOnlyUpdatedAtAndKeepsCreatedAtUnmodified()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());
        order.Ship();

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        var entry = _context.Entry(order);
        entry.State.Should().Be(EntityState.Modified);
        UpdatedAt(entry).CurrentValue.Should().Be(Now);
        UpdatedAt(entry).IsModified.Should().BeTrue();
        CreatedAt(entry).CurrentValue.Should().Be(Past);
        CreatedAt(entry).IsModified.Should().BeFalse();
    }

    [Fact]
    public void Apply_ModifiedRootWithOverwrittenCreatedAt_RevertsCreatedAtToUnmodified()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());
        var entry = _context.Entry(order);
        CreatedAt(entry).CurrentValue = Now;

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        CreatedAt(entry).IsModified.Should().BeFalse("created_at은 UPDATE 대상에서 빠져 덮어쓰이지 않는다");
        UpdatedAt(entry).CurrentValue.Should().Be(Now);
    }

    [Fact]
    public void Apply_ChangedOwnedReference_TouchesOwnerUpdatedAtAndMakesOwnerModified()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());
        order.ChangeShipping(new ShippingAddress("Busan", "48058"));

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        var entry = _context.Entry(order);
        entry.State.Should().Be(EntityState.Modified);
        UpdatedAt(entry).CurrentValue.Should().Be(Now);
        CreatedAt(entry).CurrentValue.Should().Be(Past);
        CreatedAt(entry).IsModified.Should().BeFalse();
    }

    [Fact]
    public void Apply_ModifiedNestedOwned_TouchesTopLevelNonOwnedOwner()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());
        _context.Entry(order.Shipping.Location).Property(location => location.Latitude).CurrentValue = 37.5m;

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        _context.Entry(order.Shipping).State.Should().Be(EntityState.Unchanged, "중간 owned는 바뀌지 않았다");
        _context.Entry(order).State.Should().Be(EntityState.Modified);
        UpdatedAt(_context.Entry(order)).CurrentValue.Should().Be(Now);
    }

    [Fact]
    public void Apply_AddedOwnedCollectionItem_TouchesOwner()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());
        order.AddNote("문 앞에 두세요");

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        UpdatedAt(_context.Entry(order)).CurrentValue.Should().Be(Now);
        _context.Entry(order).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public void Apply_DeletedOwnedCollectionItem_TouchesOwner()
    {
        var order = SampleDbContexts.NewOrder();
        order.AddNote("first");
        AttachUnchanged(order);
        order.ClearNotes();

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        _context.ChangeTracker.Entries<OrderNote>().Should().ContainSingle().Which.State.Should().Be(EntityState.Deleted);
        UpdatedAt(_context.Entry(order)).CurrentValue.Should().Be(Now);
    }

    [Fact]
    public void Apply_UnchangedRootWithoutOwnedChanges_LeavesEverythingUnchanged()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        _context.Entry(order).State.Should().Be(EntityState.Unchanged);
        UpdatedAt(_context.Entry(order)).CurrentValue.Should().Be(Past);
    }

    [Fact]
    public void Apply_DeletedRoot_DoesNotTouchAuditValues()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());
        _context.Orders.Remove(order);

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        var entry = _context.Entry(order);
        entry.State.Should().Be(EntityState.Deleted);
        CreatedAt(entry).CurrentValue.Should().Be(Past);
        UpdatedAt(entry).CurrentValue.Should().Be(Past);
    }

    // ---- 엣지 ----

    [Fact]
    public void Apply_AutoDetectChangesDisabled_StillDetectsChangesBeforeJudgingState()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        order.Ship();

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        _context.Entry(order).State.Should().Be(EntityState.Modified);
        UpdatedAt(_context.Entry(order)).CurrentValue.Should().Be(Now);
    }

    [Fact]
    public void Apply_OwnedChangeAndRootChangeTogether_StampsOwnerOnceWithSameNow()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder());
        order.Ship();
        order.ChangeShipping(new ShippingAddress("Busan", "48058"));
        order.AddNote("fragile");

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        UpdatedAt(_context.Entry(order)).CurrentValue.Should().Be(Now);
        CreatedAt(_context.Entry(order)).IsModified.Should().BeFalse();
    }

    [Fact]
    public void Apply_UpdatedAtAlreadyEqualToNow_StillMarksUpdatedAtModified()
    {
        var order = AttachUnchanged(SampleDbContexts.NewOrder(), updatedAt: Now);
        order.ChangeShipping(new ShippingAddress("Busan", "48058"));

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        UpdatedAt(_context.Entry(order)).IsModified.Should().BeTrue("값이 같아도 소유자 행 UPDATE(→ xmin 검사)가 일어나야 한다");
        _context.Entry(order).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public void Apply_NowWithPlus9Offset_StoresSameInstantWithZeroOffset()
    {
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);
        var kstNow = new DateTimeOffset(2026, 9, 27, 12, 4, 5, TimeSpan.FromHours(9));

        AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, kstNow);

        var createdAt = (DateTimeOffset)CreatedAt(_context.Entry(order)).CurrentValue!;
        createdAt.Offset.Should().Be(TimeSpan.Zero);
        createdAt.UtcDateTime.Should().Be(kstNow.UtcDateTime);
    }

    [Fact]
    public void Apply_NoTrackedEntries_DoesNothing()
    {
        var act = () => AuditSaveChangesInterceptor.ApplyAuditValues(_context.ChangeTracker, Now);

        act.Should().NotThrow();
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public void Apply_NullChangeTracker_ThrowsArgumentNullException()
    {
        var act = () => AuditSaveChangesInterceptor.ApplyAuditValues(null!, Now);

        act.Should().Throw<ArgumentNullException>();
    }

    // ---- 인터셉터 경로 · 시간대 ----

    [Fact]
    public void SaveChanges_WithInterceptorInLocalPlus9TimeZone_StoresUtcWithZeroOffset()
    {
        var time = CreateKstTimeProvider();
        using var context = SampleDbContexts.CreateWrite(new AuditSaveChangesInterceptor(time), new SuppressSaveInterceptor());
        var order = SampleDbContexts.NewOrder();
        context.Orders.Add(order);

        context.SaveChanges();

        var createdAt = (DateTimeOffset)CreatedAt(context.Entry(order)).CurrentValue!;
        createdAt.Offset.Should().Be(TimeSpan.Zero, "Npgsql은 오프셋이 0이 아닌 DateTimeOffset을 timestamptz에 쓰지 않는다");
        createdAt.Should().Be(time.GetUtcNow());
        time.LocalTimeZone.BaseUtcOffset.Should().Be(TimeSpan.FromHours(9));
    }

    [Fact]
    public async Task SaveChangesAsync_WithInterceptor_AppliesSameRules()
    {
        var time = CreateKstTimeProvider();
        using var context = SampleDbContexts.CreateWrite(new AuditSaveChangesInterceptor(time), new SuppressSaveInterceptor());
        var order = SampleDbContexts.NewOrder();
        context.Orders.Add(order);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var entry = context.Entry(order);
        ((DateTimeOffset)CreatedAt(entry).CurrentValue!).Offset.Should().Be(TimeSpan.Zero);
        UpdatedAt(entry).CurrentValue.Should().Be(CreatedAt(entry).CurrentValue);
    }

    [Fact]
    public void SaveChanges_CalledTwiceAfterTimeAdvances_RecomputesUpdatedAt()
    {
        var time = CreateKstTimeProvider();
        using var context = SampleDbContexts.CreateWrite(new AuditSaveChangesInterceptor(time), new SuppressSaveInterceptor());
        var order = SampleDbContexts.NewOrder();
        context.Orders.Add(order);
        context.SaveChanges();
        var first = UpdatedAt(context.Entry(order)).CurrentValue;

        time.Advance(TimeSpan.FromSeconds(5));
        context.SaveChanges();

        // 저장을 건너뛰었으므로 여전히 Added다. 재시도처럼 다시 실행되면 시각이 다시 계산된다(database.md, 허용).
        UpdatedAt(context.Entry(order)).CurrentValue.Should().Be(time.GetUtcNow()).And.NotBe(first);
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new AuditSaveChangesInterceptor(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static FakeTimeProvider CreateKstTimeProvider()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 27, 9, 30, 0, TimeSpan.FromHours(9)));
        time.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("KST", TimeSpan.FromHours(9), "KST", "KST"));
        return time;
    }

    private static PropertyEntry CreatedAt(EntityEntry entry) => entry.Property(ShadowPropertyNames.CreatedAt);

    private static PropertyEntry UpdatedAt(EntityEntry entry) => entry.Property(ShadowPropertyNames.UpdatedAt);

    // DB에서 읽어 온 것처럼 Unchanged로 추적한다. OwnsMany 항목의 키(int)는 추적 뒤 바꿀 수 없어 추적 전에 채운다.
    private Order AttachUnchanged(Order order, DateTimeOffset? updatedAt = null)
    {
        var noteKey = 1;
        _context.ChangeTracker.TrackGraph(order, node =>
        {
            if (node.Entry.Entity is OrderNote)
            {
                node.Entry.Property("Id").CurrentValue = noteKey++;
            }

            node.Entry.State = EntityState.Unchanged;
        });

        var entry = _context.Entry(order);
        CreatedAt(entry).CurrentValue = Past;
        UpdatedAt(entry).CurrentValue = updatedAt ?? Past;
        _context.ChangeTracker.AcceptAllChanges();
        return order;
    }
}
