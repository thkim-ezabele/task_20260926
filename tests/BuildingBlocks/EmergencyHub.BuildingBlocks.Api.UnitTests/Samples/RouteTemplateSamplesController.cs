using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>
/// 경로 매개변수가 있는 라우트에서 <c>instance</c>가 라우트 템플릿인지 확인하는 표본 Controller입니다(S07-T02, ADR-0025). TestServer로 실행합니다.
/// </summary>
/// <remarks>경로 매개변수는 라우트 일치에만 쓰고 액션 인자로 받지 않습니다. 실제 Controller는 <c>ISender</c>만 받습니다(ADR-0016). 여기서는 응답 경로(실패 Result · 바인딩 오류 · 예외 · 415)만 만듭니다.</remarks>
[ApiController]
[Route(RoutePath)]
public sealed class RouteTemplateSamplesController : ControllerBase
{
    public const string RoutePath = "api/v1/route-samples";

    public const string Template = "/" + RoutePath + "/{name}";

    [HttpGet("{name}")]
    public IActionResult Find() => CommonErrors.NotFound.ToProblemResult();

    [HttpGet("{name}/count")]
    public IActionResult Count([FromQuery] int count) => Ok(count);

    [HttpGet("{name}/throw")]
    public IActionResult Throw() => throw new InvalidOperationException("boom");

    [HttpPost("{name}")]
    [Consumes("text/csv")]
    public IActionResult Import() => NoContent();

    [HttpPost("{name}/json")]
    [Consumes("application/json")]
    public IActionResult ImportJson([FromBody] SampleResponse body) => Ok(body);
}
