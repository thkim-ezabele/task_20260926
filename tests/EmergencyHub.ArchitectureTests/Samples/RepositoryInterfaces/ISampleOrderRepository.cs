using EmergencyHub.BuildingBlocks.Domain.Repositories;

namespace EmergencyHub.ArchitectureTests.Samples.RepositoryInterfaces;

/// <summary>규칙을 지킨 예: Write Repository 인터페이스가 IRepository 상속.</summary>
public interface ISampleOrderRepository : IRepository
{
    /// <summary>표본 메서드.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>개수.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken);
}
