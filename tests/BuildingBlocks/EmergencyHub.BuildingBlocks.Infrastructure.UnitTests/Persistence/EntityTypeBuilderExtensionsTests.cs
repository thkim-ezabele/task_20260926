using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04: HasUniqueIndex = HasIndex(...).IsUnique().HasDatabaseName(상수). 명명 규칙의 ix_는 CommonModelMetadataTests에서 덮어쓰임을 확인한다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class EntityTypeBuilderExtensionsTests
{
    private static readonly UniqueIndexName OrderNumberName = new("ux_orders_order_number");

    [Fact]
    public void HasUniqueIndex_SingleColumn_CreatesUniqueIndexWithGivenName()
    {
        var entity = new ModelBuilder().Entity<EnumPropertyHolder>();

        var index = entity.HasUniqueIndex(holder => holder.OrderStatus, OrderNumberName).Metadata;

        index.IsUnique.Should().BeTrue();
        index.GetDatabaseName().Should().Be("ux_orders_order_number");
        index.Properties.Select(property => property.Name).Should().Equal(nameof(EnumPropertyHolder.OrderStatus));
    }

    [Fact]
    public void HasUniqueIndex_CompositeColumns_KeepsColumnOrder()
    {
        var entity = new ModelBuilder().Entity<EnumPropertyHolder>();

        var index = entity.HasUniqueIndex(holder => new { holder.DeliveryChannels, holder.OrderStatus }, OrderNumberName).Metadata;

        index.Properties.Select(property => property.Name)
            .Should().Equal(nameof(EnumPropertyHolder.DeliveryChannels), nameof(EnumPropertyHolder.OrderStatus));
    }

    [Fact]
    public void HasUniqueIndex_NullName_ThrowsArgumentNullException()
    {
        var entity = new ModelBuilder().Entity<EnumPropertyHolder>();

        var act = () => entity.HasUniqueIndex(holder => holder.OrderStatus, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
