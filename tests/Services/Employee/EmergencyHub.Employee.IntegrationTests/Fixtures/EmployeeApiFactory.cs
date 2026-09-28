using System.Net;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.Employee.Api;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.IntegrationTests.FaultInjection;
using EmergencyHub.ServiceDefaults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
/// <item><description><c>ConfigureTestServices</c>(운영 등록 뒤): 로그 수집 싱크 → <see cref="System.TimeProvider"/> 교체 → <see cref="IIdGenerator"/> 교체
/// → 이메일 사전 조회 hook(Repository 데코레이터) → 쓰기 DbContext 인터셉터 → 추가 등록 순서입니다(S06-T06).</description></item>
/// <item><description><see cref="EmployeeApiFactoryOptions.UseKestrel"/>이면 같은 설정으로 실제 Kestrel 호스트를 하나 더 띄웁니다(루프백 임의 포트). 두 호스트는 DB · <see cref="Logs"/> ·
/// 옵션의 대역 인스턴스(hook · ID 생성기 · 인터셉터)를 함께 씁니다. TestServer는 Kestrel <c>MaxRequestBodySize</c>를 적용하지 않으므로 413의 Kestrel 경로는 이 호스트로만 확인합니다.</description></item>
/// </list>
/// 테스트 클래스는 <c>[Collection(EmployeeDatabaseCollectionDefinition.Name)]</c> + <see cref="EmployeeDatabaseTest"/> 상속으로 시작 전 데이터를 비우고,
/// 테스트 안에서 <c>await using var factory = new EmployeeApiFactory(Database, options);</c>로 만듭니다(호스트는 첫 <c>CreateClient</c> · <c>Services</c> 때 시작).
/// </remarks>
public sealed class EmployeeApiFactory : WebApplicationFactory<Program>
{
    private readonly EmployeeDatabaseFixture _database;
    private readonly EmployeeApiFactoryOptions _options;
    private readonly string _contentRoot;
    private IHost? _kestrelHost;
    private Uri? _kestrelAddress;

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

    /// <summary>실제 Kestrel 호스트의 기준 주소(<c>http://127.0.0.1:{임의 포트}/</c>)입니다. 처음 읽을 때 두 호스트를 시작합니다.</summary>
    /// <exception cref="InvalidOperationException"><see cref="EmployeeApiFactoryOptions.UseKestrel"/>이 <see langword="false"/>인 경우.</exception>
    public Uri KestrelAddress
    {
        get
        {
            EnsureKestrelEnabled();
            _ = Services; // 호스트 시작(CreateHost가 Kestrel 주소를 정함)
            return _kestrelAddress ?? throw new InvalidOperationException("Kestrel 호스트 주소를 얻지 못했습니다.");
        }
    }

    /// <summary>Kestrel 호스트의 서비스 공급자입니다(TestServer 호스트의 <c>Services</c>와 다른 인스턴스, 같은 DB).</summary>
    /// <exception cref="InvalidOperationException"><see cref="EmployeeApiFactoryOptions.UseKestrel"/>이 <see langword="false"/>인 경우.</exception>
    public IServiceProvider KestrelServices
    {
        get
        {
            _ = KestrelAddress;
            return (_kestrelHost ?? throw new InvalidOperationException("Kestrel 호스트가 없습니다.")).Services;
        }
    }

    /// <summary>Kestrel 호스트로 실제 소켓 요청을 보내는 클라이언트를 만듭니다(<see cref="KestrelAddress"/> 기준, 프록시 · 리디렉션 없음). 호출자가 폐기합니다.</summary>
    /// <returns>HTTP 클라이언트.</returns>
    /// <exception cref="InvalidOperationException"><see cref="EmployeeApiFactoryOptions.UseKestrel"/>이 <see langword="false"/>인 경우.</exception>
    public HttpClient CreateKestrelClient()
    {
        var address = KestrelAddress;
        var handler = new SocketsHttpHandler { UseProxy = false, AllowAutoRedirect = false };
        return new HttpClient(handler, disposeHandler: true) { BaseAddress = address };
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        if (_kestrelHost is { } kestrelHost)
        {
            _kestrelHost = null;
            await kestrelHost.StopAsync(CancellationToken.None);
            kestrelHost.Dispose();
        }

        await base.DisposeAsync();
    }

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

            if (_options.IdGenerator is { } idGenerator)
            {
                services.RemoveAll<IIdGenerator>();
                services.AddSingleton(idGenerator);
            }

            if (_options.AfterEmailLookup is { } afterEmailLookup)
            {
                services.Decorate<IEmployeeRepository>((inner, _) => new EmailLookupHookRepository(inner, afterEmailLookup));
            }

            if (_options.WriteInterceptors.Count > 0)
            {
                services.AddWriteDbContextInterceptors([.. _options.WriteInterceptors]);
            }

            _options.ConfigureServices?.Invoke(services);
        });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="EmployeeApiFactoryOptions.UseKestrel"/>이면 TestServer 호스트를 먼저 만든 뒤 같은 빌더에 Kestrel(루프백 임의 포트)을 덧붙여 두 번째 호스트를 만들고 시작합니다
    /// (.NET 8 <c>WebApplicationFactory</c>에는 Kestrel 옵션이 없어 같은 빌더를 두 번 빌드하는 방식). Kestrel 호스트를 먼저 시작해야 주소 기능이 채워집니다.
    /// </remarks>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!_options.UseKestrel)
        {
            return base.CreateHost(builder);
        }

        var testHost = builder.Build();

        builder.ConfigureWebHost(web => web.UseKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0)));
        var kestrelHost = builder.Build();
        kestrelHost.Start();
        _kestrelHost = kestrelHost;

        var addresses = kestrelHost.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses;
        _kestrelAddress = new Uri(addresses.Single(), UriKind.Absolute);

        testHost.Start();
        return testHost;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ApiContentRoot.Delete(_contentRoot);
    }

    private void EnsureKestrelEnabled()
    {
        if (!_options.UseKestrel)
        {
            throw new InvalidOperationException("Kestrel 호스트는 EmployeeApiFactoryOptions.UseKestrel = true일 때만 있습니다.");
        }
    }
}
