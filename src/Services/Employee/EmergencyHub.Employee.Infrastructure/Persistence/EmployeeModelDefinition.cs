using System.Reflection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.Employee.Infrastructure.Persistence;

/// <summary>
/// Employee 모델 정의입니다. 쓰기(<see cref="EmployeeDbContext"/>) · 읽기(<see cref="EmployeeReadDbContext"/>) DbContext가 같은 인스턴스를 씁니다.
/// </summary>
internal sealed class EmployeeModelDefinition : IDbModelDefinition
{
    private EmployeeModelDefinition()
    {
    }

    public static EmployeeModelDefinition Instance { get; } = new();

    public IReadOnlyCollection<Assembly> StronglyTypedIdAssemblies { get; } = [typeof(EmployeeId).Assembly];

    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeModelDefinition).Assembly);
}
