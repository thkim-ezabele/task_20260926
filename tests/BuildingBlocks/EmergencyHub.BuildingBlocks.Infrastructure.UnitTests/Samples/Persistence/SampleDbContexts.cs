using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// 공통 옵션 구성(<c>UseBuildingBlocksNpgsql</c>: Npgsql + 재시도 실행 전략 + snake_case)과 더미 연결 문자열로 샘플 DbContext를 만든다.
/// 등록 확장(AddWriteDbContext · AddReadDbContext)과 같은 옵션 경로다. 모델 생성 · 변경 추적 · 스크립트 생성은 연결을 열지 않는다(DB 없음).
/// 쓰기 등록이 붙이는 감사 인터셉터는 테스트가 필요할 때 직접 넘긴다.
/// </remarks>
public static class SampleDbContexts
{
    public const string DummyConnectionString = "Host=localhost;Database=emergency_hub_sample;Username=sample_app";

    public static SampleWriteDbContext CreateWrite(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<SampleWriteDbContext>()
            .UseBuildingBlocksNpgsql(DummyConnectionString)
            .AddInterceptors(interceptors)
            .Options);

    public static SampleReadDbContext CreateRead() =>
        new(new DbContextOptionsBuilder<SampleReadDbContext>()
            .UseBuildingBlocksNpgsql(DummyConnectionString)
            .Options);

    public static Order NewOrder(string orderNumber = "ORD-0001") =>
        Order.Place(OrderId.New(), orderNumber, customerId: null, new ShippingAddress("Seoul", "04524"));
}
