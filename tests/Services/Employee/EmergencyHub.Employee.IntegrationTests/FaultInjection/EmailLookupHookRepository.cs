using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>
/// 운영 <see cref="IEmployeeRepository"/>를 감싸, DB 이메일 사전 조회가 끝난 뒤(결과 반환 전) hook을 실행하는 테스트 전용 데코레이터입니다
/// (S06-T06 동시 경합 (a): "사전 조회 뒤 커밋 전 충돌 행 삽입").
/// </summary>
/// <remarks>
/// <see cref="Fixtures.EmployeeApiFactoryOptions.AfterEmailLookup"/>가 있을 때만 <see cref="Fixtures.EmployeeApiFactory"/>가 <c>ConfigureTestServices</c>에서 붙입니다.
/// 조회 결과와 나머지 동작은 바꾸지 않습니다. hook이 던진 예외는 그대로 전파됩니다(전역 예외 처리 → 500).
/// </remarks>
/// <param name="inner">운영 Repository.</param>
/// <param name="afterLookup">조회 뒤 실행할 hook(조회한 정규화 이메일 목록, 취소 토큰).</param>
public sealed class EmailLookupHookRepository(
    IEmployeeRepository inner,
    Func<IReadOnlyCollection<string>, CancellationToken, Task> afterLookup) : IEmployeeRepository
{
    /// <inheritdoc/>
    public async Task<List<string>> ListExistingNormalizedEmailsAsync(IReadOnlyCollection<string> normalizedEmails, CancellationToken cancellationToken)
    {
        var existing = await inner.ListExistingNormalizedEmailsAsync(normalizedEmails, cancellationToken);
        await afterLookup(normalizedEmails, cancellationToken);
        return existing;
    }

    /// <inheritdoc/>
    public void Add(Domain.Employees.Employee employee) => inner.Add(employee);

    /// <inheritdoc/>
    public void AddRange(IEnumerable<Domain.Employees.Employee> employees) => inner.AddRange(employees);
}
