using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.ArchitectureTests.Samples.ControllerDependencies;

/// <summary>위반 예: Controller 액션이 Read Repository(<c>IReadRepository</c> 파생 인터페이스)를 <c>[FromServices]</c>로 받는다.</summary>
[ApiController]
[Route("api/v1/read-repository-samples")]
public sealed class ReadRepositoryActionSampleController : ControllerBase
{
    /// <summary>이름을 Read Repository로 직접 조회한다.</summary>
    /// <param name="repository">Read Repository.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>이름.</returns>
    [HttpGet]
    public Task<string?> Find([FromServices] ISampleReadRepository repository, CancellationToken cancellationToken) =>
        repository.FindNameAsync(Guid.Empty, cancellationToken);
}
