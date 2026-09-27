using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.ArchitectureTests.Samples.ControllerDependencies;

/// <summary>위반 예: Controller가 메서드 본문에서 Infrastructure 형식을 쓴다(시그니처에는 드러나지 않음).</summary>
[ApiController]
[Route("api/v1/infrastructure-samples")]
public sealed class InfrastructureUsingSampleController : ControllerBase
{
    /// <summary>Infrastructure 형식으로 인덱스 이름을 만든다.</summary>
    /// <returns>인덱스 이름.</returns>
    [HttpGet]
    public string IndexName() => new UniqueIndexName("ux_sample").Value;
}
