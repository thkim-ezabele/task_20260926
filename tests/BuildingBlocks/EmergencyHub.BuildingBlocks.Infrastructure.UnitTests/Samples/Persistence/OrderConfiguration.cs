using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.HasUniqueIndex(order => order.OrderNumber, SampleIndexNames.OrdersOrderNumber);
        builder.HasIndex(order => order.CustomerId);
        builder.Property(order => order.OrderStatus).HasCodeCheckConstraint();
        builder.Property(order => order.PreviousOrderStatus).HasCodeCheckConstraint();
        builder.Property(order => order.DeliveryChannels).HasFlagsCheckConstraint();
        builder.OwnsOne(order => order.Shipping, shipping => shipping.OwnsOne(address => address.Location));
        builder.OwnsMany(order => order.Notes, notes => notes.ToTable("order_notes"));
    }
}
