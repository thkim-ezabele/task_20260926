using System.Reflection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>테이블 · 컬럼 이름을 명시로 덮어쓰고, owned 속성에도 ck_ 도우미를 거는 모델 정의(S02-T04 인수).</remarks>
public sealed class DispatchModelDefinition : IDbModelDefinition
{
    public static DispatchModelDefinition Instance { get; } = new();

    public IReadOnlyCollection<Assembly> StronglyTypedIdAssemblies { get; } = [typeof(DispatchId).Assembly];

    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Dispatch>(dispatch =>
        {
            dispatch.ToTable("dispatch_jobs");
            dispatch.HasKey(entity => entity.Id);
            dispatch.Property(entity => entity.State).HasColumnName("job_state").HasCodeCheckConstraint();
            dispatch.Property(entity => entity.Channels).HasFlagsCheckConstraint();
            dispatch.OwnsOne(entity => entity.Route, route =>
            {
                route.Property(value => value.RouteStatus).HasCodeCheckConstraint();
                route.Property(value => value.RouteChannels).HasFlagsCheckConstraint();
            });
        });
}
