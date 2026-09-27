using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;

namespace EmergencyHub.Employee.Application.Employees;

/// <summary>
/// 직원 Read Repository입니다. 읽기 전용 DbContext에서 응답 <c>record</c>로 바로 프로젝션합니다(ADR-0007, ADR-0009).
/// </summary>
/// <remarks>구현은 Infrastructure에 두고 쿼리만 담습니다. 감사 시각은 shadow property에서 읽고 <c>xmin</c>은 노출하지 않습니다.</remarks>
public interface IEmployeeReadRepository : IReadRepository
{
    /// <summary>직원 한 명을 조회합니다.</summary>
    /// <param name="id">직원 ID 값.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>조회한 직원. 없으면 <see langword="null"/>(Handler가 22001로 바꿉니다).</returns>
    Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
