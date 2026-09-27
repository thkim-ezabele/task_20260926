using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.Employee.Infrastructure.Persistence;

/// <summary>
/// Employee 쓰기 DbContext입니다(<c>ConnectionStrings:Write</c>). Command · UnitOfWork · 마이그레이션이 씁니다(ADR-0009).
/// </summary>
/// <remarks>등록은 <see cref="EmployeeInfrastructureServiceCollectionExtensions"/>가 합니다. 마이그레이션은 이 형식 기준입니다.</remarks>
/// <param name="options">DbContext 옵션.</param>
public sealed class EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : WriteDbContextBase(options)
{
    /// <inheritdoc/>
    protected override IDbModelDefinition ModelDefinition => EmployeeModelDefinition.Instance;
}
