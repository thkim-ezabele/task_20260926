using System.Reflection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// 쓰기 · 읽기 샘플 DbContext가 함께 쓰는 모델 정의. 이 테스트 어셈블리에는 다른 설정도 있을 수 있어
/// ApplyConfigurationsFromAssembly 대신 ApplyConfiguration으로 하나만 적용한다.
/// </remarks>
public sealed class SampleModelDefinition : IDbModelDefinition
{
    public static SampleModelDefinition Instance { get; } = new();

    public IReadOnlyCollection<Assembly> StronglyTypedIdAssemblies { get; } = [typeof(OrderId).Assembly];

    public void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ApplyConfiguration(new OrderConfiguration());
}
