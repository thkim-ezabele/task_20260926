using EmergencyHub.ArchitectureTests.Samples.Fixtures;

namespace EmergencyHub.ArchitectureTests.Samples.RepositoryImplementations;

/// <summary>위반 예: 기반 클래스 없이 Write Repository 인터페이스를 구현.</summary>
public sealed class StandaloneWriteRepository : ISampleWriteRepository
{
    /// <inheritdoc />
    public Task<bool> ExistsAsync(SampleId id, CancellationToken cancellationToken) => throw new NotSupportedException();
}
