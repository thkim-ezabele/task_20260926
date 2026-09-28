using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>
/// 운영 <see cref="IEmployeeReadRepository"/>를 감싸 이름 조회만 예외로 바꾸는 테스트 전용 데코레이터입니다(S07-T03 완료 조건 ④ 500 경로, PRD-002 NFR-04).
/// </summary>
/// <remarks>
/// 예외 메시지에 조회한 이름을 일부러 넣어, 전역 예외 처리기 · 요청 완료 로그 · 추적 span이 메시지를 남기지 않는지 확인합니다.
/// 목록 · 개수 조회는 그대로 위임합니다. <see cref="Fixtures.EmployeeApiFactoryOptions.ConfigureServices"/>에서 Scrutor <c>Decorate</c>로 붙입니다.
/// </remarks>
/// <param name="inner">운영 Read Repository.</param>
public sealed class ThrowingNameLookupReadRepository(IEmployeeReadRepository inner) : IEmployeeReadRepository
{
    /// <inheritdoc/>
    public Task<List<EmployeeContactResponse>> ListOrderedByJoinedOnAsync(int skip, int take, CancellationToken cancellationToken) =>
        inner.ListOrderedByJoinedOnAsync(skip, take, cancellationToken);

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken cancellationToken) => inner.CountAsync(cancellationToken);

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">항상(메시지에 이름 값을 담음).</exception>
    public Task<EmployeeContactResponse?> FindFirstByNameAsync(Name name, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);

        throw new InvalidOperationException("이름 조회 실패: " + name.Value);
    }
}
