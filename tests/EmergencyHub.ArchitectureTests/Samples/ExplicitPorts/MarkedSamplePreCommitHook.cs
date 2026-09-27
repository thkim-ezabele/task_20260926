using EmergencyHub.BuildingBlocks.Domain.Repositories;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

namespace EmergencyHub.ArchitectureTests.Samples.ExplicitPorts;

/// <summary>위반 예: 커밋 전 훅이 IRepository 마커를 구현.</summary>
public sealed class MarkedSamplePreCommitHook : IPreCommitHook, IRepository
{
    /// <inheritdoc />
    public Task BeforeCommitAsync(WriteDbContextBase context, CancellationToken cancellationToken) => Task.CompletedTask;
}
