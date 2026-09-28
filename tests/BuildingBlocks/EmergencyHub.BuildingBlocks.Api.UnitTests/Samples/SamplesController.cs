using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>
/// OpenAPI 문서 생성 확인용 Controller입니다. 테스트 어셈블리가 진입 어셈블리라 MVC가 이 형식을 찾습니다.
/// 실제 Controller는 <c>ISender</c>만 받습니다(ADR-0016). 여기서는 문서 스키마만 봅니다.
/// </summary>
[ApiController]
[Route("api/v1/samples")]
public sealed class SamplesController : ControllerBase
{
    [HttpGet("{status}")]
    [ProducesResponseType(typeof(SampleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<SampleResponse> Get(SampleStatus status, [FromQuery] SampleChannels channels) =>
        new SampleResponse(status, channels, Note: null);
}
