using EmergencyHub.BuildingBlocks.Domain.Repositories;

namespace EmergencyHub.ArchitectureTests.Samples.Fixtures;

/// <summary>표본용 Write Repository 인터페이스(규칙 범위 밖 공용 픽스처).</summary>
public interface ISampleWriteRepository : IRepository
{
    /// <summary>표본 메서드.</summary>
    /// <param name="id">ID.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>존재 여부.</returns>
    Task<bool> ExistsAsync(SampleId id, CancellationToken cancellationToken);
}
