namespace EmergencyHub.ArchitectureTests.Samples.RepositoryInterfaces;

/// <summary>위반 예: 마커를 상속하지 않은 Repository 인터페이스(자동 등록에서 빠짐).</summary>
public interface IUnmarkedOrderRepository
{
    /// <summary>표본 메서드.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>개수.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken);
}
