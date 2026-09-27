using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.Persistence.ReadRepositories;

/// <summary>
/// 직원 Read Repository입니다. 감사 시각은 shadow property에서 읽고 <c>xmin</c>은 노출하지 않습니다.
/// </summary>
internal sealed class EmployeeReadRepository(EmployeeReadDbContext db)
    : ReadRepositoryBase<EmployeeReadDbContext>(db), IEmployeeReadRepository
{
    public Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>()
            .Where(employee => employee.Id == new EmployeeId(id))
            .Select(employee => new EmployeeResponse(
                employee.Id.Value,
                employee.DisplayName,
                employee.Email,
                employee.EmployeeStatus,
                EF.Property<DateTimeOffset>(employee, ShadowPropertyNames.CreatedAt),
                EF.Property<DateTimeOffset>(employee, ShadowPropertyNames.UpdatedAt)))
            .FirstOrDefaultAsync(cancellationToken);
}
