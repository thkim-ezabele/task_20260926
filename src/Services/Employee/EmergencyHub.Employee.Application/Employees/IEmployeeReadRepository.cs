using EmergencyHub.BuildingBlocks.Application.Persistence;

namespace EmergencyHub.Employee.Application.Employees;

/// <summary>
/// 직원 Read Repository입니다. 읽기 전용 DbContext에서 응답 <c>record</c>로 바로 프로젝션합니다(ADR-0007, ADR-0009).
/// </summary>
/// <remarks>
/// 구현은 Infrastructure에 두고 쿼리만 담습니다. PRD-001 샘플 조회(<c>GetByIdAsync</c>)는 S05-T04에서 샘플 Query와 함께 지웠고,
/// 목록 · 개수 · 이름 조회는 S05-T06에서 추가합니다(그때까지 멤버 없음).
/// </remarks>
public interface IEmployeeReadRepository : IReadRepository
{
}
