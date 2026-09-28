using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.Persistence.Repositories;

/// <summary>
/// 직원 Write Repository입니다. 정규화 이메일은 호출자가 넘긴 값을 그대로 비교합니다(ux_employees_normalized_email 인덱스 사용).
/// </summary>
/// <remarks>
/// <see cref="ListExistingNormalizedEmailsAsync"/>의 람다 <c>Contains</c>는 EF Core 8 + Npgsql에서 배열 매개변수 1개의 <c>= ANY</c>로 번역됩니다(S05-T06 실측).
/// </remarks>
internal sealed class EmployeeRepository(EmployeeDbContext db) : RepositoryBase<EmployeeDbContext>(db), IEmployeeRepository
{
    public Task<List<string>> ListExistingNormalizedEmailsAsync(IReadOnlyCollection<string> normalizedEmails, CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>()
            .Where(employee => normalizedEmails.Contains(employee.NormalizedEmail))
            .Select(employee => employee.NormalizedEmail)
            .ToListAsync(cancellationToken);

    public void Add(EmployeeAggregate employee) => Db.Set<EmployeeAggregate>().Add(employee);

    public void AddRange(IEnumerable<EmployeeAggregate> employees) => Db.Set<EmployeeAggregate>().AddRange(employees);
}
