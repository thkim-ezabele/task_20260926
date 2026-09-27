using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

namespace EmergencyHub.ArchitectureTests.Samples.MigrationServiceDependencies;

/// <summary>규칙을 지킨 예: MigrationService가 Infrastructure 형식만 쓴다(ADR-0024 <c>&lt;Service&gt;.MigrationService</c> 행 참조 가능).</summary>
public sealed class InfrastructureOnlyMigrationType
{
    /// <summary>인덱스 이름.</summary>
    public UniqueIndexName? IndexName { get; init; }
}
