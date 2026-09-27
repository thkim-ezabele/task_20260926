using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

namespace EmergencyHub.ArchitectureTests.Samples.RepositoryImplementations;

/// <summary>규칙을 지킨 예: 읽기 Repository가 ReadRepositoryBase 파생.</summary>
/// <param name="db">읽기 DbContext.</param>
public sealed class SampleReadRepository(SampleReadDbContext db) : ReadRepositoryBase<SampleReadDbContext>(db), ISampleReadRepository
{
    /// <inheritdoc />
    public Task<string?> FindNameAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
}
