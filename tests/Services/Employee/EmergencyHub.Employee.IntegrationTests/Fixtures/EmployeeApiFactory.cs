using EmergencyHub.Employee.Api;
using EmergencyHub.ServiceDefaults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// Employee Api를 실제 파이프라인(Program 등록 · 미들웨어 · Controller)으로 띄우는 <see cref="WebApplicationFactory{TEntryPoint}"/>입니다
/// (testing-strategy.md "WebApplicationFactory 도우미", S03-T07).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>DB는 컬렉션 fixture(<see cref="EmployeeDatabaseFixture"/>)의 컨테이너를 씁니다. 마이그레이션은 fixture가 이미 적용했고 이 호스트는 하지 않습니다(TD-011).</description></item>
/// <item><description>연결 문자열 · 환경 · OTLP 엔드포인트(빈 값 = exporter 미등록)는 <c>UseSetting</c>으로 넣어 Program이 등록할 때 이미 보입니다.</description></item>
/// <item><description>콘텐츠 루트는 싱크를 뺀 설정 파일 사본(<see cref="ApiContentRoot"/>)이라 콘솔 · 파일 로그를 쓰지 않습니다. 로그는 <see cref="Logs"/>로만 받습니다.</description></item>
/// <item><description><c>ConfigureTestServices</c>(운영 등록 뒤): 로그 수집 싱크 → <see cref="System.TimeProvider"/> 교체 → 쓰기 DbContext 인터셉터 → 추가 등록 순서입니다.</description></item>
/// </list>
/// 테스트 클래스는 <c>[Collection(EmployeeDatabaseCollectionDefinition.Name)]</c> + <see cref="EmployeeDatabaseTest"/> 상속으로 시작 전 데이터를 비우고,
/// 테스트 안에서 <c>await using var factory = new EmployeeApiFactory(Database, options);</c>로 만듭니다(호스트는 첫 <c>CreateClient</c> · <c>Services</c> 때 시작).
/// </remarks>
public sealed class EmployeeApiFactory : WebApplicationFactory<Program>
{
    private readonly EmployeeDatabaseFixture _database;
    private readonly EmployeeApiFactoryOptions _options;
    private readonly string _contentRoot;

    /// <summary>팩터리를 만듭니다. 호스트는 아직 시작하지 않습니다.</summary>
    /// <param name="database">컬렉션 fixture(준비 완료).</param>
    /// <param name="options">테스트 설정. <see langword="null"/>이면 Development · fixture 기본 연결입니다.</param>
    /// <exception cref="ArgumentNullException"><paramref name="database"/>가 <see langword="null"/>인 경우.</exception>
    public EmployeeApiFactory(EmployeeDatabaseFixture database, EmployeeApiFactoryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(database);

        _database = database;
        _options = options ?? new EmployeeApiFactoryOptions();
        _contentRoot = ApiContentRoot.Create(RepositoryFiles.EmployeeApiDirectory);
    }

    /// <summary>호스트가 남긴 Serilog 이벤트입니다(호스트 시작 로그 포함, 필요하면 <see cref="SerilogEventCollector.Clear"/>).</summary>
    public SerilogEventCollector Logs { get; } = new();

    /// <summary>호스트 콘텐츠 루트(설정 파일 사본 폴더)입니다.</summary>
    public string ContentRoot => _contentRoot;

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(_options.Environment);
        builder.UseContentRoot(_contentRoot);
        builder.UseSetting(EmployeeDatabaseFixture.WriteConnectionKey, _options.WriteConnectionString ?? _database.WriteConnectionString);
        builder.UseSetting(EmployeeDatabaseFixture.ReadConnectionKey, _options.ReadConnectionString ?? _database.ReadConnectionString);
        builder.UseSetting(OtlpEndpoint.ConfigurationKey, string.Empty);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(Logs);

            if (_options.TimeProvider is { } timeProvider)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(timeProvider);
            }

            if (_options.WriteInterceptors.Count > 0)
            {
                services.AddWriteDbContextInterceptors([.. _options.WriteInterceptors]);
            }

            _options.ConfigureServices?.Invoke(services);
        });
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ApiContentRoot.Delete(_contentRoot);
    }
}
