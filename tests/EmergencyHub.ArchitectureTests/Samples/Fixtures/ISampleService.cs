using EmergencyHub.BuildingBlocks.Application.Services;

namespace EmergencyHub.ArchitectureTests.Samples.Fixtures;

/// <summary>표본용 서비스 인터페이스(규칙 범위 밖 공용 픽스처).</summary>
public interface ISampleService : IService
{
    /// <summary>표본 메서드.</summary>
    /// <returns>값.</returns>
    int NextValue();
}
