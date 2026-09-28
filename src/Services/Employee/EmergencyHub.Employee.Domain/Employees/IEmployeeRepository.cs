using EmergencyHub.BuildingBlocks.Domain.Repositories;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 Write Repository입니다(쓰기 DbContext). 구현은 Infrastructure에 두고 쿼리만 담습니다(ADR-0009).
/// </summary>
/// <remarks>저장은 부르지 않습니다. 트랜잭션 데코레이터 → <c>IUnitOfWork</c>가 커밋합니다(ADR-0014).</remarks>
public interface IEmployeeRepository : IRepository
{
    /// <summary>
    /// 넘긴 정규화 이메일 중 이미 저장된 값을 돌려줍니다(일괄 등록의 DB 이메일 중복 사전 조회, PRD-002 FR-06 · ADR-0026).
    /// </summary>
    /// <remarks>
    /// 배열 매개변수 1개의 <c>normalized_email = ANY (@p)</c>로 한 번에 조회하고 <c>ux_employees_normalized_email</c>을 씁니다. 쓰기 연결로 갑니다.
    /// 값을 정규화 · 중복 제거하지 않으므로 호출자가 <see cref="Email.NormalizedEmail"/> 값을 넘깁니다. 결과 순서는 정하지 않습니다.
    /// </remarks>
    /// <param name="normalizedEmails">정규화한 이메일 목록(<see cref="Email.NormalizedEmail"/>). 비어 있으면 결과도 비어 있습니다.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>이미 저장된 정규화 이메일 목록.</returns>
    Task<List<string>> ListExistingNormalizedEmailsAsync(IReadOnlyCollection<string> normalizedEmails, CancellationToken cancellationToken);

    /// <summary>새 직원을 변경 추적에 추가합니다.</summary>
    /// <param name="employee">등록한 직원.</param>
    void Add(Employee employee);

    /// <summary>새 직원 여러 명을 변경 추적에 추가합니다(일괄 등록, 한 트랜잭션 최대 1,000개, ADR-0026).</summary>
    /// <param name="employees">등록한 직원들.</param>
    void AddRange(IEnumerable<Employee> employees);
}
