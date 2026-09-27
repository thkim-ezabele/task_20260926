using EmergencyHub.BuildingBlocks.Application.Persistence;

namespace EmergencyHub.ArchitectureTests.Samples.Fixtures;

/// <summary>표본용 Read Repository 인터페이스(규칙 범위 밖 공용 픽스처).</summary>
public interface ISampleReadRepository : IReadRepository
{
    /// <summary>표본 메서드.</summary>
    /// <param name="id">ID.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>이름.</returns>
    Task<string?> FindNameAsync(Guid id, CancellationToken cancellationToken);
}
