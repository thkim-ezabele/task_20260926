using EmergencyHub.BuildingBlocks.Application.Identifiers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// <see cref="EmployeeApiFactory"/>가 Api 운영 등록(<c>Program.ConfigureServices</c>)에 더하는 테스트 설정입니다.
/// 운영 코드를 바꾸지 않고 환경 · 연결 문자열 변형 · 시간 고정 · 장애 주입 인터셉터 · 추가 등록만 넣습니다.
/// </summary>
public sealed record EmployeeApiFactoryOptions
{
    /// <summary>호스트 환경 이름입니다. 기본값 <c>Development</c>(Swagger 노출, <c>ValidateOnBuild</c> · <c>ValidateScopes</c>)입니다.</summary>
    public string Environment { get; init; } = "Development";

    /// <summary>쓰기 연결 문자열입니다. <see langword="null"/>이면 fixture 기본값(<see cref="EmployeeDatabaseFixture.WriteConnectionString"/>)입니다.</summary>
    public string? WriteConnectionString { get; init; }

    /// <summary>읽기 연결 문자열입니다. <see langword="null"/>이면 fixture 기본값(<see cref="EmployeeDatabaseFixture.ReadConnectionString"/>)입니다.</summary>
    public string? ReadConnectionString { get; init; }

    /// <summary>
    /// 운영 <see cref="System.TimeProvider"/> 등록을 모두 지우고 대신 넣는 값입니다(예: <c>FakeTimeProvider</c>). 감사 인터셉터 · Handler가 이 값을 받습니다.
    /// </summary>
    public TimeProvider? TimeProvider { get; init; }

    /// <summary>
    /// 운영 <see cref="IIdGenerator"/> 등록(Scoped, UUIDNext)을 모두 지우고 대신 넣는 싱글턴입니다(예: <c>ScriptedIdGenerator</c>, S06-T06 같은 ID 재전송).
    /// </summary>
    public IIdGenerator? IdGenerator { get; init; }

    /// <summary>
    /// 일괄 등록 Handler의 DB 이메일 사전 조회(<c>IEmployeeRepository.ListExistingNormalizedEmailsAsync</c>)가 결과를 돌려준 <b>뒤</b>, 커밋 전에 실행할 hook입니다
    /// (S06-T06 동시 경합 (a)). 인자는 조회한 정규화 이메일 목록과 요청 취소 토큰입니다. <see langword="null"/>이면 Repository를 감싸지 않습니다.
    /// </summary>
    /// <remarks>
    /// 테스트 호스트(<c>ConfigureTestServices</c>)에서만 Repository 데코레이터(<see cref="FaultInjection.EmailLookupHookRepository"/>)로 붙습니다. 제품 코드에는 분기가 없습니다.
    /// hook 안에서 다른 스코프 · 연결로 충돌 행을 커밋하면(예: <c>EmployeeCommits.AddAndCommitAsync(factory.Services, …)</c>) 요청 트랜잭션의 INSERT가 23505를 받습니다.
    /// </remarks>
    public Func<IReadOnlyCollection<string>, CancellationToken, Task>? AfterEmailLookup { get; init; }

    /// <summary>
    /// <see langword="true"/>이면 TestServer 호스트와 별도로 같은 설정의 실제 Kestrel 호스트를 루프백 임의 포트에 띄웁니다
    /// (<see cref="EmployeeApiFactory.KestrelAddress"/> · <see cref="EmployeeApiFactory.CreateKestrelClient"/>, S06-T06 <c>MaxRequestBodySize</c> 413).
    /// </summary>
    public bool UseKestrel { get; init; }

    /// <summary>쓰기 DbContext에 덧붙일 인터셉터입니다(<see cref="DbContextInterceptorRegistration.AddWriteDbContextInterceptors"/>, 운영 등록 뒤).</summary>
    public IReadOnlyList<IInterceptor> WriteInterceptors { get; init; } = [];

    /// <summary>운영 등록 · 위 교체 <b>뒤에</b> 실행할 추가 등록입니다(예: Read Repository 대역).</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }
}
