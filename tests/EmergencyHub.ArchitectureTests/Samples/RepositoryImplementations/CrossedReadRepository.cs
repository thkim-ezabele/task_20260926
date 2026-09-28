using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

namespace EmergencyHub.ArchitectureTests.Samples.RepositoryImplementations;

/// <summary>위반 예: 읽기 Repository가 쓰기 기반(RepositoryBase)에서 파생.</summary>
/// <param name="db">쓰기 DbContext.</param>
public sealed class CrossedReadRepository(SampleWriteDbContext db) : RepositoryBase<SampleWriteDbContext>(db), ISampleReadRepository
{
    /// <inheritdoc />
    public Task<string?> FindNameAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
}
