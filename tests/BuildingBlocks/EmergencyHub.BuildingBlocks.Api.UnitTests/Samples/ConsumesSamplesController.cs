using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>
/// <c>[Consumes]</c> 불일치 415 확인용 표본 Controller입니다(S06-T01, ADR-0028). TestServer로 실행합니다.
/// </summary>
/// <remarks>
/// <see cref="Import"/>는 일괄 등록(<c>POST /api/employee</c>)처럼 본문을 <c>[FromBody]</c>로 받지 않고 Content-Type 4종을 받습니다.
/// <see cref="ImportJson"/>은 <c>[FromBody]</c>로 받는 경우(입력 포맷터 경로)입니다. 실제 Controller는 <c>ISender</c>만 받습니다(ADR-0016).
/// </remarks>
[ApiController]
[Route(RoutePath)]
public sealed class ConsumesSamplesController : ControllerBase
{
    public const string RoutePath = "api/v1/consumes-samples";

    public const string JsonBodyPath = "json-body";

    public const string MissingPath = "missing";

    [HttpPost]
    [Consumes("multipart/form-data", "application/x-www-form-urlencoded", "text/csv", "application/json")]
    public IActionResult Import() => NoContent();

    [HttpPost(JsonBodyPath)]
    [Consumes("application/json")]
    public IActionResult ImportJson([FromBody] SampleResponse body) => Ok(body);

    [HttpGet(MissingPath)]
    public IActionResult Missing() => NotFound();
}
