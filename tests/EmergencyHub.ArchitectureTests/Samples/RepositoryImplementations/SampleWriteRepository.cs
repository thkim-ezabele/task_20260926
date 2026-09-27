using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

namespace EmergencyHub.ArchitectureTests.Samples.RepositoryImplementations;

/// <summary>규칙을 지킨 예: 쓰기 Repository가 RepositoryBase 파생.</summary>
/// <param name="db">쓰기 DbContext.</param>
public sealed class SampleWriteRepository(SampleWriteDbContext db) : RepositoryBase<SampleWriteDbContext>(db), ISampleWriteRepository
{
    /// <inheritdoc />
    public Task<bool> ExistsAsync(SampleId id, CancellationToken cancellationToken) => throw new NotSupportedException();
}
