using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

public sealed class DispatchReadDbContext(DbContextOptions<DispatchReadDbContext> options) : ReadDbContextBase(options)
{
    public DbSet<Dispatch> Dispatches => Set<Dispatch>();

    protected override IDbModelDefinition ModelDefinition => DispatchModelDefinition.Instance;
}
