using EmergencyHub.BuildingBlocks.Application.Persistence;

namespace EmergencyHub.ArchitectureTests.Samples.RepositoryInterfaces;

/// <summary>규칙을 지킨 예: Read Repository 인터페이스가 IReadRepository 상속.</summary>
public interface ISampleOrderReadRepository : IReadRepository
{
    /// <summary>표본 메서드.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>개수.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken);
}
