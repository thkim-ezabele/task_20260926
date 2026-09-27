using EmergencyHub.BuildingBlocks.Domain.Events;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Auditing;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Acceptance;

// PRD-001 FR-06 "EF Core 공통 설정" 인수 시나리오(S02-T04, DB 없음).
// 단위 테스트(Persistence/*)는 샘플 Aggregate 하나(Order)의 기본 이름으로 규칙을 확인한다. 여기서는 서비스 매핑이 흔히 하는 변형
// (ToTable · HasColumnName 덮어쓰기, owned 속성의 ck_, long [Flags], 다른 Aggregate ID 참조)을 가진 두 번째 Aggregate(Dispatch)로
// 규칙이 샘플 하나에 맞춰져 있지 않은지, 그리고 ADR-0008 저장 형식(smallint / integer / bigint)과 모델 변환기 왕복을 확인한다.
// 실제 DB 동작(체크 제약 거부 · 인터셉터 저장 · xmin 충돌 · 읽기 연결 거부)은 S03-T05 통합 테스트가 확인한다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class CommonModelRulesAcceptanceTests : IDisposable
{
    private const string WideChannelsMask = "4611687117939015681";

    private readonly DispatchWriteDbContext _dispatch = CreateDispatchWrite();
    private readonly SampleWriteDbContext _order = SampleDbContexts.CreateWrite();

    public void Dispose()
    {
        _dispatch.Dispose();
        _order.Dispose();
    }

    private IModel DispatchModel => _dispatch.GetService<IDesignTimeModel>().Model;

    private IModel OrderModel => _order.GetService<IDesignTimeModel>().Model;

    private IEntityType DispatchType => DispatchModel.FindEntityType(typeof(Dispatch))!;

    private ITable DispatchTable => DispatchModel.GetRelationalModel().FindTable("dispatch_jobs", null)!;

    // ---- ck_: 최종 메타데이터 이름 ----

    [Fact]
    public void CheckConstraints_WithRenamedTableColumnAndOwnedProperties_UseFinalTableAndColumnNames()
    {
        DispatchTable.CheckConstraints.Select(check => (check.Name, check.Sql)).Should().BeEquivalentTo(
        [
            ("ck_dispatch_jobs_job_state", "job_state IN (1, 2, 3)"),
            ("ck_dispatch_jobs_channels", $"channels >= 0 AND (channels & ~{WideChannelsMask}) = 0"),
            ("ck_dispatch_jobs_route_route_status", "route_route_status IN (1, 2, 3)"),
            ("ck_dispatch_jobs_route_route_channels", "route_route_channels >= 0 AND (route_route_channels & ~11) = 0"),
        ]);
    }

    [Fact]
    public void CreateScript_OfRenamedTable_ContainsEveryCheckConstraintDdl()
    {
        var script = _dispatch.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE dispatch_jobs (");
        script.Should().Contain("CONSTRAINT ck_dispatch_jobs_job_state CHECK (job_state IN (1, 2, 3))");
        script.Should().Contain($"CONSTRAINT ck_dispatch_jobs_channels CHECK (channels >= 0 AND (channels & ~{WideChannelsMask}) = 0)");
        script.Should().Contain("CONSTRAINT ck_dispatch_jobs_route_route_status CHECK (route_route_status IN (1, 2, 3))");
        script.Should().Contain("CONSTRAINT ck_dispatch_jobs_route_route_channels CHECK (route_route_channels >= 0 AND (route_route_channels & ~11) = 0)");
        script.Should().NotContain("xmin", "xmin은 시스템 컬럼이다");
    }

    // ---- ADR-0008: 정수 저장 형식 ----

    [Theory]
    [InlineData("order_status", "smallint", false)]
    [InlineData("previous_order_status", "smallint", true)]
    [InlineData("delivery_channels", "integer", false)]
    public void CodeAndFlagsColumns_OfOrders_AreStoredAsIntegerTypes(string column, string storeType, bool nullable)
    {
        var orders = OrderModel.GetRelationalModel().FindTable("orders", null)!;

        orders.FindColumn(column)!.StoreType.Should().Be(storeType, "코드는 smallint, 31비트 이하 [Flags]는 integer다(database.md)");
        orders.FindColumn(column)!.IsNullable.Should().Be(nullable);
    }

    [Theory]
    [InlineData("job_state", "smallint")]
    [InlineData("channels", "bigint")]
    [InlineData("route_route_status", "smallint")]
    [InlineData("route_route_channels", "integer")]
    public void CodeAndFlagsColumns_OfDispatchJobs_AreStoredAsIntegerTypes(string column, string storeType)
    {
        DispatchTable.FindColumn(column)!.StoreType.Should().Be(storeType, "문자열 코드 저장은 금지다(ADR-0008)");
    }

    [Fact]
    public void EnumProperties_HaveNoStringConverter()
    {
        var enumProperties = OrderModel.GetEntityTypes().Concat(DispatchModel.GetEntityTypes())
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
            .ToList();

        enumProperties.Should().HaveCount(7);
        enumProperties.Should().OnlyContain(property => property.GetProviderClrType() != typeof(string));
    }

    // ---- 강타입 ID: 모델에 등록된 변환기 왕복 ----

    public static TheoryData<Guid> RoundTripGuids => [Guid.Empty, Guid.Parse("0192f0c4-8a2b-7c3d-9e4f-5a6b7c8d9e0f")];

    [Theory]
    [MemberData(nameof(RoundTripGuids))]
    public void ModelConverter_OfDispatchKey_RoundTripsIncludingEmptyGuid(Guid value)
    {
        var converter = DispatchType.FindPrimaryKey()!.Properties.Single().GetValueConverter()!;

        var provider = converter.ConvertToProvider(new DispatchId(value));

        provider.Should().Be(value);
        converter.ConvertFromProvider(provider).Should().Be(new DispatchId(value));
    }

    [Theory]
    [MemberData(nameof(RoundTripGuids))]
    public void ModelConverter_OfOtherAggregateReference_RoundTripsIncludingEmptyGuid(Guid value)
    {
        var property = DispatchType.FindProperty(nameof(Dispatch.SourceOrderId))!;
        var converter = property.GetValueConverter()!;

        property.GetColumnType().Should().Be("uuid");
        property.IsNullable.Should().BeFalse();
        converter.ConvertFromProvider(converter.ConvertToProvider(new OrderId(value))).Should().Be(new OrderId(value));
    }

    [Theory]
    [MemberData(nameof(RoundTripGuids))]
    public void ModelConverter_OfNullableReference_RoundTripsIncludingEmptyGuid(Guid value)
    {
        var converter = OrderModel.FindEntityType(typeof(Order))!.FindProperty(nameof(Order.CustomerId))!.GetValueConverter()!;

        converter.ConvertFromProvider(converter.ConvertToProvider(new CustomerId(value))).Should().Be(new CustomerId(value));
    }

    // ---- 두 번째 Aggregate: 공통 뒤처리 ----

    [Fact]
    public void SecondAggregate_Key_IsNeverGeneratedUuidWithoutDefault()
    {
        var id = DispatchType.FindPrimaryKey()!.Properties.Single();

        id.ValueGenerated.Should().Be(ValueGenerated.Never);
        id.GetColumnType().Should().Be("uuid");
        id.GetDefaultValueSql().Should().BeNull();
        DispatchTable.PrimaryKey!.Name.Should().Be("pk_dispatch_jobs");
    }

    [Fact]
    public void SecondAggregate_HasAuditAndXminShadowProperties_AndOwnedTypeHasNone()
    {
        DispatchType.FindProperty(ShadowPropertyNames.CreatedAt)!.IsShadowProperty().Should().BeTrue();
        DispatchType.FindProperty(ShadowPropertyNames.UpdatedAt)!.IsShadowProperty().Should().BeTrue();

        var version = DispatchType.FindProperty(ShadowPropertyNames.Version)!;
        version.IsShadowProperty().Should().BeTrue();
        version.IsConcurrencyToken.Should().BeTrue();
        version.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
        version.GetColumnName().Should().Be("xmin");
        version.GetColumnType().Should().Be("xid");

        var route = DispatchModel.FindEntityType(typeof(DispatchRoute))!;
        route.FindProperty(ShadowPropertyNames.CreatedAt).Should().BeNull();
        route.FindProperty(ShadowPropertyNames.UpdatedAt).Should().BeNull();
        route.FindProperty(ShadowPropertyNames.Version).Should().BeNull();

        DispatchTable.Columns.Select(column => column.Name).Should().BeEquivalentTo(
        [
            "id",
            "job_state",
            "channels",
            "source_order_id",
            "route_route_status",
            "route_route_channels",
            "created_at",
            "updated_at",
            "xmin",
        ]);
    }

    [Fact]
    public void SecondAggregate_DomainEvents_AreNotMapped()
    {
        DispatchType.FindProperty(nameof(Dispatch.DomainEvents)).Should().BeNull();
        DispatchType.FindNavigation(nameof(Dispatch.DomainEvents)).Should().BeNull();
        DispatchModel.GetEntityTypes().Should().NotContain(entityType => typeof(IDomainEvent).IsAssignableFrom(entityType.ClrType));
    }

    // ---- 읽기 DbContext: 두 번째 서비스에서도 같은 모델 · 저장 차단 ----

    [Fact]
    public void ReadContext_OfSecondService_HasSameCreateScriptAndBlocksSaving()
    {
        using var read = new DispatchReadDbContext(new DbContextOptionsBuilder<DispatchReadDbContext>()
            .UseBuildingBlocksNpgsql(SampleDbContexts.DummyConnectionString)
            .Options);

        read.Database.GenerateCreateScript().Should().Be(_dispatch.Database.GenerateCreateScript());
        read.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);

        read.Dispatches.Add(NewDispatch());
        var act = () => read.SaveChanges();
        act.Should().Throw<InvalidOperationException>();
    }

    // ---- 감사: 두 번째 Aggregate, 로컬 +09:00 ----

    [Fact]
    public void SaveChanges_SecondAggregateAddedInPlus9TimeZone_StampsUtcCreatedAndUpdated()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 27, 18, 0, 0, TimeSpan.FromHours(9)));
        time.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("KST", TimeSpan.FromHours(9), "KST", "KST"));
        using var context = CreateDispatchWrite(new AuditSaveChangesInterceptor(time), new SuppressSaveInterceptor());
        var dispatch = NewDispatch();
        context.Dispatches.Add(dispatch);

        context.SaveChanges();

        var entry = context.Entry(dispatch);
        var createdAt = (DateTimeOffset)entry.Property(ShadowPropertyNames.CreatedAt).CurrentValue!;
        createdAt.Offset.Should().Be(TimeSpan.Zero);
        createdAt.UtcDateTime.Should().Be(new DateTime(2026, 9, 27, 9, 0, 0, DateTimeKind.Utc));
        entry.Property(ShadowPropertyNames.UpdatedAt).CurrentValue.Should().Be(createdAt);
    }

    [Fact]
    public void ApplyAuditValues_ReplacedOwnedRouteOnUnchangedRoot_TouchesOwnerOnly()
    {
        var past = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero);
        var dispatch = NewDispatch();
        _dispatch.Attach(dispatch);
        var entry = _dispatch.Entry(dispatch);
        entry.Property(ShadowPropertyNames.CreatedAt).CurrentValue = past;
        entry.Property(ShadowPropertyNames.UpdatedAt).CurrentValue = past;
        _dispatch.ChangeTracker.AcceptAllChanges();

        _dispatch.Entry(dispatch.Route).Property(route => route.RouteStatus).CurrentValue = OrderStatus.Shipped;
        AuditSaveChangesInterceptor.ApplyAuditValues(_dispatch.ChangeTracker, now);

        entry.State.Should().Be(EntityState.Modified);
        entry.Property(ShadowPropertyNames.UpdatedAt).CurrentValue.Should().Be(now);
        entry.Property(ShadowPropertyNames.CreatedAt).CurrentValue.Should().Be(past);
        entry.Property(ShadowPropertyNames.CreatedAt).IsModified.Should().BeFalse();
    }

    private static Dispatch NewDispatch() =>
        Dispatch.Create(
            new DispatchId(Guid.NewGuid()),
            OrderId.New(),
            new DispatchRoute(OrderStatus.Placed, DeliveryChannels.Sms | DeliveryChannels.Email));

    private static DispatchWriteDbContext CreateDispatchWrite(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<DispatchWriteDbContext>()
            .UseBuildingBlocksNpgsql(SampleDbContexts.DummyConnectionString)
            .AddInterceptors(interceptors)
            .Options);
}
