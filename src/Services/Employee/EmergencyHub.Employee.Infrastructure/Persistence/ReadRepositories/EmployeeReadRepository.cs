using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.Persistence.ReadRepositories;

/// <summary>
/// 직원 Read Repository입니다(읽기 DbContext). 값 변환기 Value Object는 조건 · 정렬에서 VO끼리 비교하고, <c>.Value</c>는 최상위 <c>Select</c>에서만 씁니다(coding-conventions).
/// </summary>
internal sealed class EmployeeReadRepository(EmployeeReadDbContext db)
    : ReadRepositoryBase<EmployeeReadDbContext>(db), IEmployeeReadRepository
{
    public Task<List<EmployeeContactResponse>> ListOrderedByJoinedOnAsync(int skip, int take, CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>()
            .OrderBy(employee => employee.JoinedOn)
            .ThenBy(employee => employee.Id)
            .Skip(skip)
            .Take(take)
            .Select(employee => new EmployeeContactResponse(
                employee.Id.Value, employee.Name.Value, employee.Email.Value, employee.PhoneNumber.Value, employee.JoinedOn.Value))
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>().CountAsync(cancellationToken);

    public Task<EmployeeContactResponse?> FindFirstByNameAsync(Name name, CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>()
            .Where(employee => employee.Name == name)
            .OrderBy(employee => employee.JoinedOn)
            .ThenBy(employee => employee.Id)
            .Select(employee => new EmployeeContactResponse(
                employee.Id.Value, employee.Name.Value, employee.Email.Value, employee.PhoneNumber.Value, employee.JoinedOn.Value))
            .FirstOrDefaultAsync(cancellationToken);
}
