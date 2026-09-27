using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

public sealed class SampleReadDbContext(DbContextOptions<SampleReadDbContext> options) : ReadDbContextBase(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override IDbModelDefinition ModelDefinition => SampleModelDefinition.Instance;
}
