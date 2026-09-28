using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;

/// <summary>
/// 직원 전체 목록 한 쪽을 조회합니다(PRD-002 FR-07, <c>GET /api/employee?page={page}&amp;pageSize={pageSize}</c>). 정렬은 <c>joined_on</c> → <c>id</c> 고정입니다.
/// </summary>
/// <remarks>
/// 범위(<see cref="Page"/> 1 ~ <see cref="MaxPage"/>, <see cref="PageSize"/> <see cref="MinPageSize"/> ~ <see cref="MaxPageSize"/>)는
/// <see cref="ListEmployeesQueryValidator"/>가 1003(<c>Common.InvalidPaging</c>)으로 판정합니다(ADR-0025 예외 목록).
/// 숫자가 아닌 값은 모델 바인딩 오류 1001이라 이 Query까지 오지 않습니다.
/// </remarks>
/// <param name="Page">쪽 번호(1부터).</param>
/// <param name="PageSize">쪽 크기.</param>
public sealed record ListEmployeesQuery(int Page, int PageSize) : IQuery<ListEmployeesResponse>
{
    /// <summary><c>page</c>가 없을 때의 쪽 번호입니다.</summary>
    public const int DefaultPage = 1;

    /// <summary>쪽 번호 상한입니다(PRD-002 Q10). skip 최댓값 (100,000 - 1) × 100이 <see cref="int"/> 안에 들어갑니다.</summary>
    public const int MaxPage = 100_000;

    /// <summary><c>pageSize</c>가 없을 때의 쪽 크기입니다.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>쪽 크기 하한입니다.</summary>
    public const int MinPageSize = 1;

    /// <summary>쪽 크기 상한입니다.</summary>
    public const int MaxPageSize = 100;

    /// <summary>쿼리 문자열 값으로 Query를 만듭니다. 빠진 값은 기본값(<see cref="DefaultPage"/> · <see cref="DefaultPageSize"/>)으로 채우고, 범위는 검사하지 않습니다.</summary>
    /// <param name="page">바인딩한 <c>page</c>. 없으면 <see langword="null"/>.</param>
    /// <param name="pageSize">바인딩한 <c>pageSize</c>. 없으면 <see langword="null"/>.</param>
    /// <returns>만든 Query.</returns>
    public static ListEmployeesQuery Create(int? page, int? pageSize) => new(page ?? DefaultPage, pageSize ?? DefaultPageSize);
}
