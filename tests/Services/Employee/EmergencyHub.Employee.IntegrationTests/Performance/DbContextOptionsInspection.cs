using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.Performance;

/// <summary>호스트가 실제로 만든 DbContext 옵션을 읽습니다(S06-T06 측정 조건: <c>EnableSensitiveDataLogging</c> 끔, NFR-04).</summary>
public static class DbContextOptionsInspection
{
    /// <summary>쓰기 · 읽기 DbContext 중 하나라도 <c>EnableSensitiveDataLogging</c>이 켜져 있는지 봅니다.</summary>
    /// <param name="services">호스트 서비스 공급자(<c>factory.Services</c>).</param>
    /// <returns>하나라도 켜져 있으면 <see langword="true"/>.</returns>
    public static bool IsSensitiveDataLoggingEnabled(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        return IsEnabled(scope.ServiceProvider.GetRequiredService<EmployeeDbContext>())
            || IsEnabled(scope.ServiceProvider.GetRequiredService<EmployeeReadDbContext>());
    }

    private static bool IsEnabled(DbContext context) =>
        context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;
}
