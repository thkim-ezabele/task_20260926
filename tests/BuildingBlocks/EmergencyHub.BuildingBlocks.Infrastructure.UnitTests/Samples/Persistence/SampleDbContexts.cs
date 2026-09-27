using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// Npgsql 공급자 + 더미 연결 문자열 + snake_case로 샘플 DbContext를 만든다. 모델 생성 · 변경 추적 · 스크립트 생성은 연결을 열지 않는다(DB 없음).
/// 공통 등록 확장(UseNpgsql · UseSnakeCaseNamingConvention · 인터셉터)은 S02-T07이 만들고, 여기서는 같은 구성을 직접 조립한다.
/// </remarks>
public static class SampleDbContexts
{
    public const string DummyConnectionString = "Host=localhost;Database=emergency_hub_sample;Username=sample_app";

    public static SampleWriteDbContext CreateWrite(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<SampleWriteDbContext>()
            .UseNpgsql(DummyConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptors)
            .Options);

    public static SampleReadDbContext CreateRead() =>
        new(new DbContextOptionsBuilder<SampleReadDbContext>()
            .UseNpgsql(DummyConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

    public static Order NewOrder(string orderNumber = "ORD-0001") =>
        Order.Place(OrderId.New(), orderNumber, customerId: null, new ShippingAddress("Seoul", "04524"));
}
