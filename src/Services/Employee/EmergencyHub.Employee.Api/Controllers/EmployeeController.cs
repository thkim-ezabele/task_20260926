using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.Employee.Api.Employees.Import;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;
using EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.Employee.Api.Controllers;

/// <summary>
/// 과제 명세의 직원 API입니다(PRD-002, 규칙 예외는 ADR-0025). <see cref="ISender"/>만 받고 입력 → Command 변환, 결과 → 응답 변환만 합니다(ADR-0016).
/// </summary>
/// <remarks>경로는 버전 없는 단수형 <c>/api/employee</c>입니다(ADR-0025 예외 목록). 일괄 등록(S06-T05) · 목록 조회(S07-T01) · 이름 조회(S07-T02)입니다.</remarks>
/// <param name="sender">Command / Query 디스패처.</param>
[ApiController]
[Route(RoutePath)]
public sealed class EmployeeController(ISender sender) : ControllerBase
{
    /// <summary>라우트 경로입니다.</summary>
    public const string RoutePath = "api/employee";

    /// <summary>이름 조회의 액션 라우트 템플릿입니다. 합친 템플릿은 <c>api/employee/{name}</c>입니다.</summary>
    internal const string NameRouteTemplate = "{name}";

    /// <summary>일괄 등록이 받는 Content-Type 4종입니다(ADR-0025 "요청 형식", ADR-0026 1절). 순서는 OpenAPI 문서 순서입니다.</summary>
    internal static readonly IReadOnlyList<string> RegisterContentTypes = [MultipartFormData, FormUrlEncoded, TextCsv, ApplicationJson];

    private const string MultipartFormData = "multipart/form-data";
    private const string FormUrlEncoded = "application/x-www-form-urlencoded";
    private const string TextCsv = "text/csv";
    private const string ApplicationJson = "application/json";

    /// <summary>
    /// CSV · JSON 입력으로 직원을 한 번에 등록합니다(PRD-002 FR-05 · FR-06). 한 행이라도 실패하면 아무것도 저장하지 않습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 입력은 multipart 파일 필드 <c>file</c> · 텍스트 필드 <c>data</c>, form-urlencoded <c>data</c>, raw body(<c>text/csv</c> · <c>application/json</c>) 중 하나입니다.
    /// 전용 바인더가 꺼내고(<see cref="EmployeeImportPayloadBinder"/>), 폼 값 공급자는 쓰지 않습니다(<see cref="DisableFormValueProvidersAttribute"/>).
    /// </para>
    /// <para>
    /// 성공은 <c>201</c> + <c>{ count, ids }</c>이고 <c>Location</c>은 없습니다(ADR-0025). 크기 한도는 본문 전체 기준 1 MiB이며
    /// 넘으면 <c>413</c> · <c>1004</c>, 지원하지 않는 Content-Type은 <c>415</c> · <c>1005</c>입니다(NFR-01, ADR-0028).
    /// </para>
    /// </remarks>
    /// <param name="payload">전용 바인더가 만든 입력.</param>
    /// <param name="cancellationToken">요청 취소 토큰.</param>
    /// <returns>201 + 등록 결과, 실패면 ProblemDetails(400 · 409 · 413 · 415).</returns>
    [HttpPost]
    [Consumes(MultipartFormData, FormUrlEncoded, TextCsv, ApplicationJson)]
    [RequestSizeLimit(EmployeeImportLimits.MaxRequestBodyBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = EmployeeImportLimits.MaxRequestBodyBytes, ValueLengthLimit = EmployeeImportLimits.MaxRequestBodyBytes)]
    [DisableFormValueProviders]
    [ProducesResponseType<RegisterEmployeesResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ErrorProblemDetails.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ErrorProblemDetails.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, ErrorProblemDetails.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, ErrorProblemDetails.ContentType)]
    public async Task<ActionResult<RegisterEmployeesResponse>> RegisterAsync(EmployeeImportPayload payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var result = await sender.SendAsync(new RegisterEmployeesCommand(payload.Format, payload.Sources, payload.Content), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// 직원 전체 목록 한 쪽을 돌려줍니다(PRD-002 FR-07). 정렬은 입사일 → ID(등록 순) 고정입니다.
    /// </summary>
    /// <remarks>
    /// <c>page</c>(1부터, 기본 1, 1 ~ 100,000) · <c>pageSize</c>(기본 20, 1 ~ 100)는 <c>int?</c>로 받아 빠진 값의 기본값을 Query가 채웁니다(ADR-0025).
    /// 숫자가 아니거나 <see cref="int"/> 범위 밖이면 바인딩 오류 <c>400</c> · <c>1001</c>, 범위 밖이면 <c>400</c> · 필드 코드 <c>1003</c>입니다.
    /// 마지막 쪽을 넘으면 빈 <c>items</c>와 올바른 <c>totalCount</c>의 <c>200</c>입니다.
    /// </remarks>
    /// <param name="page">쪽 번호.</param>
    /// <param name="pageSize">쪽 크기.</param>
    /// <param name="cancellationToken">요청 취소 토큰.</param>
    /// <returns>200 + <c>{ items, totalCount, page, pageSize }</c>, 실패면 ProblemDetails(400).</returns>
    [HttpGet]
    [ProducesResponseType<ListEmployeesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ErrorProblemDetails.ContentType)]
    public async Task<ActionResult<ListEmployeesResponse>> ListAsync([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await sender.QueryAsync(ListEmployeesQuery.Create(page, pageSize), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 이름이 정확히 같은 직원 한 명의 연락정보를 돌려줍니다(PRD-002 FR-08). 동명이인이면 입사일이 빠른 1명(같으면 등록 순)입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 이름은 앞뒤 공백 제거 + NFC 뒤 정확히 비교합니다(대소문자 구분, Validator · Handler의 Name Value Object). 공백을 뗀 뒤 비었으면 <c>400</c> · 21007,
    /// 그 밖의 이름 규칙 실패는 <c>400</c> · 21008 / 21009, 없으면 <c>404</c> · 22001입니다. <c>/</c>가 들어간 이름(<c>%2F</c>)은 보장하지 않습니다.
    /// </para>
    /// <para>
    /// 이름은 개인정보라 요청 완료 로그 · <c>ProblemDetails.instance</c> · 추적 span <c>url.path</c>에는 요청 경로 대신 라우트 템플릿
    /// <c>/api/employee/{name}</c>이 남습니다(ADR-0025 "이름 경로 매개변수와 개인정보").
    /// </para>
    /// </remarks>
    /// <param name="name">경로의 이름(라우팅이 퍼센트 디코딩한 값). 공백만 있으면 단순 형식 바인더가 <see langword="null"/>로 바꿉니다(<c>ConvertEmptyStringToNull</c>, 400 · 21007).</param>
    /// <param name="cancellationToken">요청 취소 토큰.</param>
    /// <returns>200 + <c>{ id, name, email, tel, joined }</c>, 실패면 ProblemDetails(400 · 404).</returns>
    [HttpGet(NameRouteTemplate)]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ErrorProblemDetails.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ErrorProblemDetails.ContentType)]
    public async Task<ActionResult<EmployeeResponse>> GetByNameAsync(string? name, CancellationToken cancellationToken)
    {
        var result = await sender.QueryAsync(new GetEmployeeByNameQuery(name), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Ok(result.Value);
    }
}
