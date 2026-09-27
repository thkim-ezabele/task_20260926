namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// DB 통합 테스트의 기반 클래스입니다. 테스트마다 시작 전 <see cref="EmployeeDatabaseFixture.ResetAsync"/>로 데이터를 비웁니다(ADR-0022).
/// 파생 클래스에는 <c>[Collection(EmployeeDatabaseCollectionDefinition.Name)]</c>을 붙입니다.
/// </summary>
/// <param name="database">컬렉션 fixture.</param>
public abstract class EmployeeDatabaseTest(EmployeeDatabaseFixture database) : IAsyncLifetime
{
    /// <summary>컬렉션 fixture입니다.</summary>
    protected EmployeeDatabaseFixture Database { get; } = database;

    /// <summary>테스트 취소 토큰입니다.</summary>
    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <inheritdoc/>
    public virtual async ValueTask InitializeAsync() => await Database.ResetAsync(CancellationToken);

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
