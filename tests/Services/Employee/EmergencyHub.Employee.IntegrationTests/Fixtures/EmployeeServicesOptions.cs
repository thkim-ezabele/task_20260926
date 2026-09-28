using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// <see cref="EmployeeDatabaseFixture.CreateServices"/>가 운영 등록 코드(<c>AddEmployeeInfrastructure</c>)에 더하는 테스트 설정입니다.
/// 운영 코드를 바꾸지 않고 재시도 한도 축소 · 장애 주입 인터셉터 · 연결 문자열 변형 · 시간 고정만 넣습니다(testing-strategy.md "장애 주입").
/// </summary>
public sealed record EmployeeServicesOptions
{
    /// <summary>
    /// 재시도 설정(<c>AddEmployeeInfrastructure</c>의 <c>retry</c> 인자에 그대로 넘김). <see langword="null"/>이면 Npgsql 기본값(6회 · 30초)입니다.
    /// 재시도 한도 테스트(P6)는 작은 값(예: 2회 · 10ms)을 넣어 시간을 줄입니다.
    /// </summary>
    public DbRetryOptions? Retry { get; init; }

    /// <summary>쓰기 연결 문자열입니다. <see langword="null"/>이면 fixture 기본값(<see cref="EmployeeDatabaseFixture.WriteConnectionString"/>)입니다.</summary>
    public string? WriteConnectionString { get; init; }

    /// <summary>읽기 연결 문자열입니다. <see langword="null"/>이면 fixture 기본값(<see cref="EmployeeDatabaseFixture.ReadConnectionString"/>)입니다.</summary>
    public string? ReadConnectionString { get; init; }

    /// <summary>
    /// 운영 등록보다 <b>먼저</b> 넣는 <see cref="System.TimeProvider"/>입니다(예: <c>FakeTimeProvider</c>). 운영 등록은 <c>TryAddSingleton</c>이라 먼저 넣은 값이 쓰입니다.
    /// </summary>
    public TimeProvider? TimeProvider { get; init; }

    /// <summary>쓰기 DbContext에 덧붙일 인터셉터입니다(<see cref="DbContextInterceptorRegistration.AddWriteDbContextInterceptors"/>).</summary>
    public IReadOnlyList<IInterceptor> WriteInterceptors { get; init; } = [];

    /// <summary>읽기 DbContext에 덧붙일 인터셉터입니다(<see cref="DbContextInterceptorRegistration.AddReadDbContextInterceptors"/>).</summary>
    public IReadOnlyList<IInterceptor> ReadInterceptors { get; init; } = [];

    /// <summary>운영 등록 <b>뒤에</b> 실행할 추가 등록입니다(예: 테스트용 <c>IPreCommitHook</c>).</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }
}
