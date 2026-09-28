using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

public sealed class DispatchWriteDbContext(DbContextOptions<DispatchWriteDbContext> options) : WriteDbContextBase(options)
{
    public DbSet<Dispatch> Dispatches => Set<Dispatch>();

    protected override IDbModelDefinition ModelDefinition => DispatchModelDefinition.Instance;
}
