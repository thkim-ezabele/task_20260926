using EmergencyHub.BuildingBlocks.Domain.Repositories;

namespace EmergencyHub.ArchitectureTests.Samples.RepositoryInterfaces;

/// <summary>위반 예: Read Repository가 쓰기 마커(IRepository)를 상속.</summary>
public interface IMismarkedOrderReadRepository : IRepository
{
    /// <summary>표본 메서드.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>개수.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken);
}
