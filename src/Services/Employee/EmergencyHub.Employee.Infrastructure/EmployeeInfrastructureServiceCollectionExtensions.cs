using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Application;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.Infrastructure;

/// <summary>
/// Employee 서비스 등록 진입점입니다(database.md "공통 DbContext 등록", ADR-0010 · 0011 · 0014).
/// </summary>
/// <remarks>
/// 재시도 설정(<see cref="DbRetryOptions"/>)은 여기서 공통 등록 확장에 넘기는 한 경로뿐입니다. 호출자가 <c>UseNpgsql</c>을 다시 부르지 않습니다(BL-073).
/// </remarks>
public static class EmployeeInfrastructureServiceCollectionExtensions
{
    private const string WriteConnectionName = "Write";
    private const string ReadConnectionName = "Read";

    /// <summary>
    /// Api용 등록입니다: 공통 인프라 → 규칙 기반 등록(Employee Application · Infrastructure 어셈블리) → 쓰기 · 읽기 DbContext → UnitOfWork(정규화 이메일 유니크 위반 → 23001).
    /// </summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="configuration">설정(<c>ConnectionStrings:Write</c> · <c>ConnectionStrings:Read</c>).</param>
    /// <param name="retry">재시도 설정. <see langword="null"/>이면 Npgsql 기본값입니다.</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 또는 <paramref name="configuration"/>이 <see langword="null"/>인 경우.</exception>
    /// <exception cref="InvalidOperationException">연결 문자열이 없거나 비어 있는 경우, 또는 두 번 호출한 경우.</exception>
    /// <remarks>
    /// <c>AddConventionalServices</c>는 한 번만 부를 수 있어 Application 어셈블리도 여기서 함께 넘깁니다. 호스트는 이 메서드 뒤에 다시 부르지 않습니다.
    /// </remarks>
    public static IServiceCollection AddEmployeeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        DbRetryOptions? retry = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services
            .AddBuildingBlocksInfrastructure()
            .AddConventionalServices(EmployeeApplicationAssembly.Assembly, EmployeeInfrastructureAssembly.Assembly)
            .AddEmployeeWriteDbContext(configuration, retry)
            .AddReadDbContext<EmployeeReadDbContext>(configuration.GetConnectionString(ReadConnectionName), retry: retry)
            .AddUnitOfWork<EmployeeDbContext>(errors => errors.Map(EmployeeDbNames.NormalizedEmailUniqueIndex, EmployeeErrors.DuplicateEmail));
    }

    /// <summary>
    /// 쓰기 DbContext만 등록합니다. MigrationService가 이것만 부르고, <see cref="AddEmployeeInfrastructure"/>도 이 경로를 씁니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="configuration">설정(<c>ConnectionStrings:Write</c>).</param>
    /// <param name="retry">재시도 설정. <see langword="null"/>이면 Npgsql 기본값입니다.</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 또는 <paramref name="configuration"/>이 <see langword="null"/>인 경우.</exception>
    /// <exception cref="InvalidOperationException">쓰기 연결 문자열이 없거나 비어 있는 경우.</exception>
    public static IServiceCollection AddEmployeeWriteDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        DbRetryOptions? retry = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddWriteDbContext<EmployeeDbContext>(configuration.GetConnectionString(WriteConnectionName), retry: retry);
    }
}
