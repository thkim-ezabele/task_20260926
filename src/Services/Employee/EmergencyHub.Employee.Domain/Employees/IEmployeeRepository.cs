using EmergencyHub.BuildingBlocks.Domain.Repositories;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 Write Repository입니다(쓰기 DbContext). 구현은 Infrastructure에 두고 쿼리만 담습니다(ADR-0009).
/// </summary>
/// <remarks>저장은 부르지 않습니다. 트랜잭션 데코레이터 → <c>IUnitOfWork</c>가 커밋합니다(ADR-0014).</remarks>
public interface IEmployeeRepository : IRepository
{
    /// <summary>같은 정규화 이메일의 직원이 있는지 확인합니다.</summary>
    /// <param name="normalizedEmail">정규화한 이메일(<see cref="Email.NormalizedEmail"/>). <c>normalized_email</c> 저장 값과 그대로 비교합니다.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>있으면 <see langword="true"/>.</returns>
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>새 직원을 변경 추적에 추가합니다.</summary>
    /// <param name="employee">등록한 직원.</param>
    void Add(Employee employee);
}
