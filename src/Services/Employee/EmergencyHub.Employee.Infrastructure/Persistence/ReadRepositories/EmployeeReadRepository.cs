using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Application.Employees;

namespace EmergencyHub.Employee.Infrastructure.Persistence.ReadRepositories;

/// <summary>
/// 직원 Read Repository입니다(읽기 DbContext). 조회 메서드는 S05-T06에서 추가합니다(PRD-001 샘플 조회는 S05-T04에서 제거).
/// </summary>
internal sealed class EmployeeReadRepository(EmployeeReadDbContext db)
    : ReadRepositoryBase<EmployeeReadDbContext>(db), IEmployeeReadRepository;
