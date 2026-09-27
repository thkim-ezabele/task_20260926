using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.Persistence.Repositories;

/// <summary>
/// 직원 Write Repository입니다. 이메일은 호출자가 정규화한 값을 그대로 비교합니다(ux_employees_email 인덱스 사용).
/// </summary>
internal sealed class EmployeeRepository(EmployeeDbContext db) : RepositoryBase<EmployeeDbContext>(db), IEmployeeRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>().AnyAsync(employee => employee.Email == email, cancellationToken);

    public void Add(EmployeeAggregate employee) => Db.Set<EmployeeAggregate>().Add(employee);
}
