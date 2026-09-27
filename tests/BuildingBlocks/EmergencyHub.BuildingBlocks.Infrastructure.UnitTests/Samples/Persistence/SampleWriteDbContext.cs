using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

public sealed class SampleWriteDbContext(DbContextOptions<SampleWriteDbContext> options) : WriteDbContextBase(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override IDbModelDefinition ModelDefinition => SampleModelDefinition.Instance;
}
