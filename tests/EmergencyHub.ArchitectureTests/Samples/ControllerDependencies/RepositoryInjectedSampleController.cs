using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.ArchitectureTests.Samples.ControllerDependencies;

/// <summary>위반 예: Controller가 Write Repository(<c>IRepository</c> 파생 인터페이스)를 생성자로 주입받는다.</summary>
/// <param name="repository">Write Repository.</param>
[ApiController]
[Route("api/v1/repository-samples")]
public sealed class RepositoryInjectedSampleController(ISampleWriteRepository repository) : ControllerBase
{
    /// <summary>존재 여부를 Repository로 직접 조회한다.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>존재 여부.</returns>
    [HttpGet]
    public Task<bool> Exists(CancellationToken cancellationToken) => repository.ExistsAsync(default, cancellationToken);
}
