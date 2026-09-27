using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// 운영 등록이 만든 쓰기 DbContext 옵션에 테스트 인터셉터를 덧붙입니다(testing-strategy.md "장애 주입").
/// </summary>
/// <remarks>
/// EF Core 8에는 <c>ConfigureDbContext</c>가 없고, <c>UseNpgsql</c>을 다시 부르면 실행 전략 설정 한 곳 규칙(BL-073)이 깨집니다.
/// 그래서 <c>AddDbContext</c>가 등록한 <see cref="DbContextOptions{TContext}"/> 팩터리를 감싸, 운영 옵션을 그대로 복사한 뒤 <c>AddInterceptors</c>만 더합니다
/// (감사 인터셉터 등 기존 인터셉터는 유지되고 뒤에 이어 붙음). <c>WebApplicationFactory</c>의 <c>ConfigureTestServices</c>에서도 같은 방식으로 씁니다(S03-T07).
/// </remarks>
public static class DbContextInterceptorRegistration
{
    /// <summary>쓰기 DbContext(<see cref="EmployeeDbContext"/>) 옵션에 인터셉터를 덧붙입니다. 운영 등록(<c>AddEmployeeInfrastructure</c>) 뒤에 부릅니다.</summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="interceptors">덧붙일 인터셉터(등록 순서대로 실행).</param>
    /// <returns>같은 <paramref name="services"/>.</returns>
    /// <exception cref="InvalidOperationException">쓰기 DbContext 옵션이 팩터리로 한 번 등록되어 있지 않은 경우.</exception>
    public static IServiceCollection AddWriteDbContextInterceptors(this IServiceCollection services, params IInterceptor[] interceptors)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(interceptors);

        var descriptors = services.Where(descriptor => descriptor.ServiceType == typeof(DbContextOptions<EmployeeDbContext>)).ToList();
        if (descriptors is not [{ ImplementationFactory: { } originalFactory } descriptor])
        {
            throw new InvalidOperationException(
                $"DbContextOptions<{nameof(EmployeeDbContext)}> 팩터리 등록이 정확히 하나여야 합니다(찾은 개수 {descriptors.Count}). AddEmployeeInfrastructure 뒤에 부르세요.");
        }

        services.Remove(descriptor);
        services.Add(new ServiceDescriptor(
            typeof(DbContextOptions<EmployeeDbContext>),
            provider => new DbContextOptionsBuilder<EmployeeDbContext>((DbContextOptions<EmployeeDbContext>)originalFactory(provider))
                .AddInterceptors(interceptors)
                .Options,
            descriptor.Lifetime));

        return services;
    }
}
