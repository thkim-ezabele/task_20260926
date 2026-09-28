using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.Employees;

/// <summary>
/// 직원 Read Repository입니다. 읽기 전용 DbContext에서 응답 <c>record</c>로 바로 프로젝션합니다(ADR-0007, ADR-0009).
/// </summary>
/// <remarks>
/// 구현은 Infrastructure에 두고 쿼리만 담습니다. 목록과 개수는 메서드를 나누고 Handler가 합칩니다(<c>COUNT(*) OVER()</c> 미사용, PRD-002 FR-07).
/// 정렬은 모두 <c>joined_on</c> → <c>id</c>(등록 순, UUID v7)이며 <c>ix_employees_joined_on_id</c> · <c>ix_employees_name_joined_on_id</c>를 씁니다(database.md).
/// </remarks>
public interface IEmployeeReadRepository : IReadRepository
{
    /// <summary>입사일 → ID 순으로 정렬한 직원 목록 한 쪽을 돌려줍니다(PRD-002 FR-07).</summary>
    /// <param name="skip">건너뛸 행 수(0 이상). 쪽 번호 계산 · 범위 검사는 Handler · Validator가 합니다.</param>
    /// <param name="take">가져올 행 수(1 이상).</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>정렬한 직원 목록. 마지막 쪽을 넘으면 비어 있습니다.</returns>
    Task<List<EmployeeContactResponse>> ListOrderedByJoinedOnAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>전체 직원 수를 돌려줍니다(상태 무관, PRD-002 FR-07 <c>totalCount</c>).</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>전체 직원 수.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 이름이 정확히 같은(대소문자 구분) 직원 한 명을 돌려줍니다. 동명이인이면 입사일이 빠른 1명, 같으면 등록 순(ID)입니다(PRD-002 FR-08).
    /// </summary>
    /// <param name="name">검증된 이름(Trim + NFC, <see cref="Name.Create"/>).</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>찾은 직원. 없으면 <see langword="null"/>.</returns>
    Task<EmployeeContactResponse?> FindFirstByNameAsync(Name name, CancellationToken cancellationToken);
}
