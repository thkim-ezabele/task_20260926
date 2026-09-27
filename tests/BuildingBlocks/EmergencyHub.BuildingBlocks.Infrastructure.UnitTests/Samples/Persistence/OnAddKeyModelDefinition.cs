using System.Reflection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// 규칙 위반 예시: 서비스 매핑이 강타입 ID 키에 ValueGeneratedOnAdd를 명시한다. 공통 뒤처리가 Never로 되돌리는지 확인한다(ADR-0013, DB 기본값 미사용).
/// EF 8은 변환기가 있는 키에 값 생성을 붙이지 않으므로(실측) 정상 매핑만으로는 뒤처리 규칙을 판별할 수 없다.
/// </remarks>
public sealed class OnAddKeyModelDefinition : IDbModelDefinition
{
    public IReadOnlyCollection<Assembly> StronglyTypedIdAssemblies { get; } = [];

    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(entity => entity.Id);
            order.Property(entity => entity.Id).ValueGeneratedOnAdd();
        });
}
