using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>ReadRepositoryBase 확인용. 마커를 구현하지 않는 이유는 SampleOrderRepository와 같다.</remarks>
public sealed class SampleOrderReadRepository(SampleReadDbContext db) : ReadRepositoryBase<SampleReadDbContext>(db)
{
    public SampleReadDbContext ExposedDb => Db;

    public IQueryable<string> OrderNumbers() => Db.Orders.Select(order => order.OrderNumber);
}
