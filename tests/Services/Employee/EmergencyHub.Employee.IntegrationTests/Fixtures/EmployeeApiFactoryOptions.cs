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

    /// <summary>쓰기 DbContext에 덧붙일 인터셉터입니다(<see cref="DbContextInterceptorRegistration.AddWriteDbContextInterceptors"/>, 운영 등록 뒤).</summary>
    public IReadOnlyList<IInterceptor> WriteInterceptors { get; init; } = [];

    /// <summary>운영 등록 · 위 교체 <b>뒤에</b> 실행할 추가 등록입니다(예: Read Repository 대역).</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }
}
