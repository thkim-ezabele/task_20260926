using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.TestData;

/// <summary>
/// 운영 등록의 Repository · UnitOfWork로 직원을 저장하는 도우미입니다. Handler 사전 검사를 거치지 않으므로 P4(23505 인프라 경로)에도 씁니다.
/// </summary>
public static class EmployeeCommits
{
    /// <summary>새 DI 스코프(= Command 하나)에서 <c>Add</c> → <c>CommitAsync</c>를 합니다.</summary>
    /// <param name="services">fixture가 만든 서비스 공급자.</param>
    /// <param name="employee">저장할 직원.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>UnitOfWork 결과(23505 · xmin 충돌은 실패 Result, 그 밖의 예외는 그대로 전파).</returns>
    public static async Task<Result> AddAndCommitAsync(IServiceProvider services, Domain.Employees.Employee employee, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(cancellationToken);
    }
}
