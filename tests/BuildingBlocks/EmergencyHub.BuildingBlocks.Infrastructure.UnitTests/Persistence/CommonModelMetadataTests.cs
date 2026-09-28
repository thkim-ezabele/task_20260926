using System.Text;
using System.Text.RegularExpressions;
using EmergencyHub.BuildingBlocks.Domain.Events;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// PRD-001 FR-06 · S02-T04: EF Core 공통 모델 규칙을 설계 시점 모델(IDesignTimeModel)로 확인한다(DB 없음).
// 기준: database.md "EF Core 공통 모델 규칙" · "감사 컬럼" · "트랜잭션 & 동시성 제어", dba 인계 단언 A1~A12.
// 실제 DB에서의 동작(인터셉터 저장 · 체크 제약 거부 · xmin 충돌)은 S03-T05 통합 테스트가 확인한다.
[Trait("FR", "PRD-001/FR-06")]
public sealed partial class CommonModelMetadataTests : IDisposable
{
    private const int MaxIdentifierBytes = 63;

    private readonly SampleWriteDbContext _context = SampleDbContexts.CreateWrite();

    private IModel Model => _context.GetService<IDesignTimeModel>().Model;

    private IEntityType OrderType => Model.FindEntityType(typeof(Order))!;

    private IRelationalModel RelationalModel => Model.GetRelationalModel();

    private ITable OrdersTable => RelationalModel.FindTable("orders", null)!;

    public void Dispose() => _context.Dispose();

    // ---- A1 · A3: 이름 ----

    [Fact]
    public void Tables_OfSampleModel_AreSnakeCasePlural()
    {
        RelationalModel.Tables.Select(table => table.Name).Should().BeEquivalentTo(["orders", "order_notes"]);
    }

    [Fact]
    public void OrdersColumns_IncludeOwnedAuditAndXminColumns_WithoutDomainEventsColumn()
    {
        OrdersTable.Columns.Select(column => column.Name).Should().BeEquivalentTo(
        [
            "id",
            "order_number",
            "order_status",
            "previous_order_status",
            "delivery_channels",
            "customer_id",
            "shipping_city",
            "shipping_zip_code",
            "shipping_location_latitude",
            "shipping_location_longitude",
            "created_at",
            "updated_at",
            "xmin",
        ]);
    }

    [Fact]
    public void OwnsManyTable_HasNoAuditOrXminColumns()
    {
        RelationalModel.FindTable("order_notes", null)!.Columns.Select(column => column.Name)
            .Should().BeEquivalentTo(["order_id", "id", "text"]);
    }

    [Fact]
    public void KeyForeignKeyAndIndexNames_FollowSnakeCaseConvention()
    {
        OrdersTable.PrimaryKey!.Name.Should().Be("pk_orders");
        RelationalModel.FindTable("order_notes", null)!.PrimaryKey!.Name.Should().Be("pk_order_notes");
        RelationalModel.FindTable("order_notes", null)!.ForeignKeyConstraints.Select(fk => fk.Name)
            .Should().BeEquivalentTo(["fk_order_notes_orders_order_id"]);
        OrdersTable.Indexes.Select(index => index.Name)
            .Should().BeEquivalentTo(["ix_orders_customer_id", "ux_orders_order_number"]);
    }

    [Fact]
    public void AllIdentifiers_AreLowerSnakeCaseAndAtMost63Bytes()
    {
        var identifiers = AllIdentifiers().ToList();

        identifiers.Should().NotBeEmpty();
        identifiers.Should().OnlyContain(
            name => SnakeCase().IsMatch(name),
            "식별자는 따옴표가 필요 없는 소문자 snake_case다(database.md)");
        identifiers.Should().OnlyContain(
            name => Encoding.UTF8.GetByteCount(name) <= MaxIdentifierBytes,
            "PostgreSQL은 63바이트를 넘는 이름을 경고 없이 자른다");
    }

    // ---- A2: ux_ ----

    [Fact]
    public void UniqueIndex_UsesServiceConstantName_InsteadOfNamingConventionIxPrefix()
    {
        var index = OrderType.GetIndexes().Single(candidate => candidate.IsUnique);

        index.GetDatabaseName().Should().Be(SampleIndexNames.OrdersOrderNumber.Value);
        index.GetDatabaseName().Should().NotStartWith("ix_");
        index.Properties.Select(property => property.Name).Should().Equal(nameof(Order.OrderNumber));
    }

    [Fact]
    public void NonUniqueIndex_KeepsNamingConventionIxPrefix()
    {
        var index = OrderType.GetIndexes().Single(candidate => !candidate.IsUnique);

        index.GetDatabaseName().Should().Be("ix_orders_customer_id");
    }

    // ---- A4 · A5: ck_ ----

    [Fact]
    public void CheckConstraints_OnOrdersTable_AreExactlyTheDeclaredCodeAndFlagsRules()
    {
        OrdersTable.CheckConstraints.Select(check => (check.Name, check.Sql)).Should().BeEquivalentTo(
        [
            ("ck_orders_order_status", "order_status IN (1, 2, 3)"),
            ("ck_orders_previous_order_status", "previous_order_status IN (1, 2, 3)"),
            ("ck_orders_delivery_channels", "delivery_channels >= 0 AND (delivery_channels & ~11) = 0"),
        ]);
    }

    [Fact]
    public void CheckConstraintMarkers_AreRemovedFromModelAfterFinalizing()
    {
        var annotations = Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .SelectMany(property => property.GetAnnotations())
            .Select(annotation => annotation.Name);

        annotations.Should().NotContain(
            name => name.StartsWith("EmergencyHub:", StringComparison.Ordinal),
            "도우미 표식이 마이그레이션 스냅숏에 남지 않아야 한다");
    }

    // ---- A7: 강타입 ID ----

    [Fact]
    public void StronglyTypedIdKey_ConvertsToUuidAndIsNeverGenerated()
    {
        var id = OrderType.FindPrimaryKey()!.Properties.Single();

        id.Name.Should().Be(nameof(Order.Id));
        id.GetValueConverter().Should().NotBeNull();
        id.GetValueConverter()!.ProviderClrType.Should().Be<Guid>();
        id.GetColumnType().Should().Be("uuid");
        id.ValueGenerated.Should().Be(ValueGenerated.Never);

        // GetDefaultValue()는 설정이 없어도 CLR 기본값을 돌려주므로 구성 주석이 없는지로 판정한다.
        id.FindAnnotation(RelationalAnnotationNames.DefaultValue).Should().BeNull();
        id.FindAnnotation(RelationalAnnotationNames.DefaultValueSql).Should().BeNull();
        id.GetDefaultValueSql().Should().BeNull();
        id.GetValueGenerationStrategy().Should().Be(NpgsqlValueGenerationStrategy.None);
        id.GetValueGeneratorFactory().Should().BeNull();
    }

    [Fact]
    public void CreateScript_IdColumn_HasNoDatabaseDefault()
    {
        var script = _context.Database.GenerateCreateScript();
        var start = script.IndexOf("CREATE TABLE orders (", StringComparison.Ordinal);
        var ordersTable = script[start..script.IndexOf(");", start, StringComparison.Ordinal)];

        // order_notes의 id(OwnsMany 합성 int 키)는 IDENTITY라 대상이 아니다. 강타입 ID 키가 있는 orders만 본다.
        ordersTable.Should().Contain("id uuid NOT NULL,");
        ordersTable.Should().NotContain("DEFAULT").And.NotContain("IDENTITY");
        script.Should().NotContain("gen_random_uuid").And.NotContain("uuidv7");
    }

    [Fact]
    public void NullableStronglyTypedIdReference_ConvertsToNullableUuid()
    {
        var customerId = OrderType.FindProperty(nameof(Order.CustomerId))!;

        customerId.GetValueConverter().Should().NotBeNull();
        customerId.GetColumnType().Should().Be("uuid");
        customerId.IsNullable.Should().BeTrue();
        customerId.ValueGenerated.Should().Be(ValueGenerated.Never);
    }

    [Fact]
    public void OwnedTypeKeys_OfStronglyTypedId_AreNeverGenerated()
    {
        var ownedKeys = Model.GetEntityTypes()
            .Where(entityType => entityType.IsOwned())
            .SelectMany(entityType => entityType.FindPrimaryKey()!.Properties)
            .Where(property => property.ClrType == typeof(OrderId))
            .ToList();

        ownedKeys.Should().NotBeEmpty();
        ownedKeys.Should().OnlyContain(property => property.ValueGenerated == ValueGenerated.Never);
    }

    // ---- A8: 도메인 이벤트 ----

    [Fact]
    public void DomainEvents_AreNotMappedAsPropertyNavigationOrEntityType()
    {
        OrderType.FindProperty(nameof(Order.DomainEvents)).Should().BeNull();
        OrderType.FindNavigation(nameof(Order.DomainEvents)).Should().BeNull();
        OrderType.FindSkipNavigation(nameof(Order.DomainEvents)).Should().BeNull();
        OrderType.FindProperty(nameof(Order.PlacedEvent)).Should().BeNull();
        OrderType.FindNavigation(nameof(Order.PlacedEvent)).Should().BeNull("IDomainEvent 구현 형식은 탐색 대상에서도 빠진다");
        Model.GetEntityTypes().Should().NotContain(entityType => typeof(IDomainEvent).IsAssignableFrom(entityType.ClrType));
    }

    // ---- A9 · A10: 감사 ----

    [Theory]
    [InlineData(ShadowPropertyNames.CreatedAt, "created_at")]
    [InlineData(ShadowPropertyNames.UpdatedAt, "updated_at")]
    public void AuditProperties_OnRootEntity_AreRequiredShadowTimestamptz(string propertyName, string columnName)
    {
        var property = OrderType.FindProperty(propertyName)!;

        property.Should().NotBeNull();
        property.IsShadowProperty().Should().BeTrue();
        property.ClrType.Should().Be<DateTimeOffset>();
        property.IsNullable.Should().BeFalse();
        property.GetColumnName().Should().Be(columnName);
        property.GetColumnType().Should().Be("timestamp with time zone");
    }

    [Fact]
    public void OwnedTypes_HaveNoAuditOrConcurrencyShadowProperties()
    {
        var ownedTypes = Model.GetEntityTypes().Where(entityType => entityType.IsOwned()).ToList();

        ownedTypes.Select(entityType => entityType.ClrType)
            .Should().BeEquivalentTo([typeof(ShippingAddress), typeof(GeoPoint), typeof(OrderNote)]);
        ownedTypes.Should().AllSatisfy(entityType =>
        {
            entityType.FindProperty(ShadowPropertyNames.CreatedAt).Should().BeNull();
            entityType.FindProperty(ShadowPropertyNames.UpdatedAt).Should().BeNull();
            entityType.FindProperty(ShadowPropertyNames.Version).Should().BeNull();
            entityType.GetProperties().Should().NotContain(
                property => property.IsConcurrencyToken,
                "owned 변경은 소유자 updated_at을 바꿔 소유자 행의 xmin 검사로 충돌을 잡는다");
        });
    }

    // ---- A11 · A12: xmin ----

    [Fact]
    public void Version_OnRootEntity_IsShadowXminRowVersion()
    {
        var version = OrderType.FindProperty(ShadowPropertyNames.Version)!;

        version.Should().NotBeNull();
        version.IsShadowProperty().Should().BeTrue();
        version.ClrType.Should().Be<uint>();
        version.IsConcurrencyToken.Should().BeTrue();
        version.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
        version.GetColumnName().Should().Be("xmin");
        version.GetColumnType().Should().Be("xid");
    }

    [Fact]
    public void GenerateCreateScript_DoesNotCreateXminSystemColumn()
    {
        var script = _context.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE orders");
        script.Should().Contain("created_at timestamp with time zone NOT NULL");
        script.Should().Contain("CONSTRAINT ck_orders_delivery_channels CHECK (delivery_channels >= 0 AND (delivery_channels & ~11) = 0)");
        script.Should().Contain("CREATE UNIQUE INDEX ux_orders_order_number");
        script.Should().NotContain("xmin", "xmin은 시스템 컬럼이라 CREATE TABLE에 넣지 않는다");
    }

    [Fact]
    public void ShadowPropertyNames_HaveDocumentedValues()
    {
        ShadowPropertyNames.CreatedAt.Should().Be("CreatedAt");
        ShadowPropertyNames.UpdatedAt.Should().Be("UpdatedAt");
        ShadowPropertyNames.Version.Should().Be("Version");
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex SnakeCase();

    private IEnumerable<string> AllIdentifiers() =>
        RelationalModel.Tables.SelectMany(table =>
            new[] { table.Name }
                .Concat(table.Columns.Select(column => column.Name))
                .Concat(table.PrimaryKey is null ? [] : [table.PrimaryKey.Name])
                .Concat(table.UniqueConstraints.Select(constraint => constraint.Name))
                .Concat(table.ForeignKeyConstraints.Select(constraint => constraint.Name))
                .Concat(table.Indexes.Select(index => index.Name))
                .Concat(table.CheckConstraints.Select(check => check.Name!)));
}
