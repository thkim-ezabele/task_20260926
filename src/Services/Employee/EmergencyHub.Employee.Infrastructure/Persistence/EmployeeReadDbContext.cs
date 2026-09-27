using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.Employee.Infrastructure.Persistence;

/// <summary>
/// Employee 읽기 전용 DbContext입니다(<c>ConnectionStrings:Read</c>). Read Repository만 씁니다(ADR-0007, ADR-0009).
/// </summary>
/// <remarks>쓰기 DbContext와 같은 모델 정의를 씁니다. 마이그레이션 대상이 아닙니다.</remarks>
/// <param name="options">DbContext 옵션.</param>
public sealed class EmployeeReadDbContext(DbContextOptions<EmployeeReadDbContext> options) : ReadDbContextBase(options)
{
    /// <inheritdoc/>
    protected override IDbModelDefinition ModelDefinition => EmployeeModelDefinition.Instance;
}
