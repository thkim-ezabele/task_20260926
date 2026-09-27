using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.Employee.Api.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.Employee.Api.Controllers;

/// <summary>
/// 직원 API입니다(wiki/05-api/employee-api.md). <see cref="ISender"/>만 받고 요청 → Command / Query 변환, 결과 → 응답 변환만 합니다(ADR-0016).
/// </summary>
/// <param name="sender">Command / Query 디스패처.</param>
[ApiController]
[Route("api/v1/employees")]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    /// <summary>조회 엔드포인트의 라우트 이름입니다. 등록 응답의 <c>Location</c>이 이 라우트로 만들어집니다.</summary>
    public const string GetByIdRouteName = "GetEmployeeById";

    private const string ProblemContentType = "application/problem+json";

    /// <summary>직원을 등록합니다.</summary>
    /// <param name="request">등록 요청.</param>
    /// <param name="cancellationToken">요청 취소 토큰.</param>
    /// <returns>201 + <c>Location</c> + <c>{ id }</c>, 실패면 ProblemDetails(400 · 409 등).</returns>
    [HttpPost]
    [ProducesResponseType<RegisterEmployeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemContentType)]
    public async Task<ActionResult<RegisterEmployeeResponse>> RegisterAsync(RegisterEmployeeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.SendAsync(request.ToCommand(), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var id = result.Value.Value;
        return CreatedAtRoute(GetByIdRouteName, new { id }, new RegisterEmployeeResponse(id));
    }

    /// <summary>직원 한 명을 조회합니다.</summary>
    /// <param name="id">직원 ID. 경로 제약을 두지 않아 형식 오류는 바인딩 오류(1001)입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰.</param>
    /// <returns>200 + 직원, 없으면 404 · 22001.</returns>
    [HttpGet("{id}", Name = GetByIdRouteName)]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemContentType)]
    public async Task<ActionResult<EmployeeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.QueryAsync(new GetEmployeeByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Ok(result.Value);
    }
}
