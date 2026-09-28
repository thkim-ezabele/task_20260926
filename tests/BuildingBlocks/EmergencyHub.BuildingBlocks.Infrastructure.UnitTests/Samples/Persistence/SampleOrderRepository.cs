using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// RepositoryBase 확인용. 마커(IRepository)를 구현하면 이 어셈블리를 검색하는 DI 테스트의 ValidateOnBuild가
/// DbContext 등록을 요구하므로 마커를 구현하지 않는다(S02-T03 tester 인계).
/// </remarks>
public sealed class SampleOrderRepository(SampleWriteDbContext db) : RepositoryBase<SampleWriteDbContext>(db)
{
    public SampleWriteDbContext ExposedDb => Db;

    public void Add(Order order) => Db.Orders.Add(order);
}
