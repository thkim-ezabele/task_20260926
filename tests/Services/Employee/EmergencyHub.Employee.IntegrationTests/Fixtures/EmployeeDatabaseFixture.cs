using System.Security.Cryptography;
using EmergencyHub.Employee.Infrastructure;
using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// Employee 통합 테스트 컬렉션 fixture입니다. PostgreSQL 컨테이너 1개로 AppHost 로컬 DB 구성을 재현합니다
/// (testing-strategy.md "통합 테스트 · 테스트 DB 구성 (fixture)", ADR-0012 · ADR-0022, S03-T06 · S03-T07 공유).
/// </summary>
/// <remarks>
/// <para>준비 순서(한 번): 컨테이너 시작(AppHost 초기화 스크립트 공유 마운트 → <c>employee_app</c> 롤) → 슈퍼유저 연결로 AppHost와 같은 생성 스크립트 1문장
/// → 운영 적용 경로(MigrationService와 같은 등록 · 실행 전략 안 <c>MigrateAsync</c>) → <c>Respawner.CreateAsync</c>(이력 테이블 제외).</para>
/// <para>테스트마다 시작 전 <see cref="ResetAsync"/>(<see cref="EmployeeDatabaseTest"/>가 호출). 테스트는 한 컬렉션에서 순차 실행합니다(TRUNCATE 잠금 · 테스트 전용 트리거).</para>
/// <para>슈퍼유저 연결 문자열은 DB 생성에만 쓰고 공개하지 않습니다. 비밀번호는 실행마다 만든 난수이고 출력 · 로그에 남기지 않습니다.</para>
/// </remarks>
public sealed class EmployeeDatabaseFixture : IAsyncLifetime
{
    /// <summary>쓰기 연결 설정 키입니다(운영과 같음).</summary>
    public const string WriteConnectionKey = "ConnectionStrings:Write";

    /// <summary>읽기 연결 설정 키입니다(운영과 같음).</summary>
    public const string ReadConnectionKey = "ConnectionStrings:Read";

    /// <summary>읽기 연결에 붙이는 서버 옵션입니다(AppHost 읽기 연결 식과 같음).</summary>
    public const string ReadOnlyConnectionOptions = "-c default_transaction_read_only=on";

    private const string SecretCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int SecretLength = 32;
    private const string PublicSchema = "public";
    private const string MigrationHistoryTable = "__EFMigrationsHistory";
    private const string ContainerLogName = "employee-postgres";

    private PostgreSqlContainer? _container;
    private Respawner? _respawner;
    private string? _writeConnectionString;
    private string? _readConnectionString;

    /// <summary><c>employee_app</c> 쓰기 연결 문자열입니다.</summary>
    /// <exception cref="InvalidOperationException">fixture가 준비되기 전인 경우.</exception>
    public string WriteConnectionString => _writeConnectionString ?? throw NotInitialized();

    /// <summary><c>employee_app</c> 읽기 연결 문자열입니다(쓰기 + <see cref="ReadOnlyConnectionOptions"/>).</summary>
    /// <exception cref="InvalidOperationException">fixture가 준비되기 전인 경우.</exception>
    public string ReadConnectionString => _readConnectionString ?? throw NotInitialized();

    /// <summary>운영과 같은 연결 설정(<c>ConnectionStrings:Write</c> · <c>ConnectionStrings:Read</c>)입니다. <c>WebApplicationFactory</c> 설정 주입에 씁니다(S03-T07).</summary>
    public IReadOnlyDictionary<string, string?> ConnectionStringSettings => new Dictionary<string, string?>(StringComparer.Ordinal)
    {
        [WriteConnectionKey] = WriteConnectionString,
        [ReadConnectionKey] = ReadConnectionString,
    };

    /// <summary>Respawn이 만든 초기화 SQL입니다(이력 테이블 제외 확인용).</summary>
    /// <exception cref="InvalidOperationException">fixture가 준비되기 전인 경우.</exception>
    public string RespawnDeleteSql => (_respawner ?? throw NotInitialized()).DeleteSql ?? string.Empty;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var appRolePassword = NewSecret();

        _container = BuildContainer(appRolePassword);
        try
        {
            await _container.StartAsync(cancellationToken);
            await CreateDatabaseAsync(_container.GetConnectionString(), cancellationToken);

            (_writeConnectionString, _readConnectionString) = BuildConnectionStrings(_container.GetConnectionString(), appRolePassword);

            await ApplyMigrationsAsync(cancellationToken);
            _respawner = await CreateRespawnerAsync(cancellationToken);
        }
        catch
        {
            await ContainerLogs.SaveAsync(_container, ContainerLogName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is null)
        {
            return;
        }

        await ContainerLogs.SaveAsync(_container, ContainerLogName);
        NpgsqlConnection.ClearAllPools();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// 운영 적용 경로로 마이그레이션을 적용합니다: MigrationService 등록(<c>AddEmployeeWriteDbContext</c>, Npgsql 기본 재시도) → 새 스코프의 쓰기 DbContext
    /// → <c>CreateExecutionStrategy().ExecuteAsync(MigrateAsync)</c>(MigrationWorker와 같은 형태, <c>EnsureCreated</c> 금지). 준비 때 한 번 부르고, 재적용 멱등 테스트가 다시 부릅니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>적용 작업.</returns>
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmployeeWriteDbContext(BuildConfiguration(WriteConnectionString, readConnectionString: null));

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(token => db.Database.MigrateAsync(token), cancellationToken);
    }

    /// <summary>테이블 데이터를 비웁니다(Respawn, 쓰기 연결, 이력 테이블 제외). 테스트 시작 전에 부릅니다.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>초기화 작업.</returns>
    /// <exception cref="InvalidOperationException">fixture가 준비되기 전인 경우.</exception>
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        var respawner = _respawner ?? throw NotInitialized();

        await using var connection = await OpenWriteConnectionAsync(cancellationToken);
        await respawner.ResetAsync(connection);
    }

    /// <summary>
    /// 운영 등록 코드(<c>AddEmployeeInfrastructure</c>: 쓰기 · 읽기 DbContext, UnitOfWork, 규칙 기반 등록)로 서비스 공급자를 만듭니다.
    /// 로그는 <c>FakeLogCollector</c>로 모든 수준을 모읍니다(<c>GetFakeLogCollector()</c>).
    /// </summary>
    /// <param name="options">테스트 설정. <see langword="null"/>이면 fixture 기본 연결과 Npgsql 기본 재시도입니다.</param>
    /// <returns>서비스 공급자(<c>ValidateOnBuild</c> · <c>ValidateScopes</c>). 호출자가 폐기합니다.</returns>
    public ServiceProvider CreateServices(EmployeeServicesOptions? options = null)
    {
        options ??= new EmployeeServicesOptions();

        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace));
        services.AddFakeLogging();

        if (options.TimeProvider is { } timeProvider)
        {
            services.TryAddSingleton(timeProvider);
        }

        services.AddEmployeeInfrastructure(
            BuildConfiguration(options.WriteConnectionString ?? WriteConnectionString, options.ReadConnectionString ?? ReadConnectionString),
            options.Retry);

        if (options.WriteInterceptors.Count > 0)
        {
            services.AddWriteDbContextInterceptors([.. options.WriteInterceptors]);
        }

        options.ConfigureServices?.Invoke(services);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    /// <summary>컨테이너 로그를 지정한 폴더에 저장합니다(<see cref="ContainerLogs"/>, 폐기 · 시작 실패 때는 환경 변수 폴더로 자동 저장).</summary>
    /// <param name="directory">저장 폴더. 비어 있으면 저장하지 않습니다.</param>
    /// <returns>저장한 파일 경로. 저장하지 않았으면 <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">fixture가 준비되기 전인 경우.</exception>
    public Task<string?> SaveContainerLogsAsync(string? directory) =>
        ContainerLogs.SaveAsync(_container ?? throw NotInitialized(), ContainerLogName, directory);

    /// <summary>쓰기 연결 문자열을 바꾼 사본을 만듭니다(예: P1 <c>Options=-c default_transaction_isolation=serializable</c>, 잘못된 비밀번호).</summary>
    /// <param name="configure">연결 문자열 빌더 변경.</param>
    /// <returns>바뀐 연결 문자열.</returns>
    public string WriteConnectionStringWith(Action<NpgsqlConnectionStringBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new NpgsqlConnectionStringBuilder(WriteConnectionString);
        configure(builder);
        return builder.ConnectionString;
    }

    /// <summary>쓰기 연결(<c>employee_app</c>)을 열어 돌려줍니다. 검증 쿼리 · 트리거 생성에 씁니다. 호출자가 폐기합니다.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>열린 연결.</returns>
    public Task<NpgsqlConnection> OpenWriteConnectionAsync(CancellationToken cancellationToken) => OpenAsync(WriteConnectionString, cancellationToken);

    /// <summary>읽기 연결(<c>employee_app</c>, 읽기 전용 트랜잭션 기본값)을 열어 돌려줍니다. 호출자가 폐기합니다.</summary>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>열린 연결.</returns>
    public Task<NpgsqlConnection> OpenReadConnectionAsync(CancellationToken cancellationToken) => OpenAsync(ReadConnectionString, cancellationToken);

    private static PostgreSqlContainer BuildContainer(string appRolePassword)
    {
        var builder = new PostgreSqlBuilder(PostgresImage.Name)
            .WithPassword(NewSecret())
            .WithEnvironment(EmployeeDatabaseSettings.AppRolePasswordVariable, appRolePassword);

        // AppHost WithInitFiles와 같은 폴더의 파일을 복사본 없이 그대로 넣는다(시작 전 복사, 0644면 엔트리포인트가 source로 실행).
        foreach (var script in Directory.EnumerateFiles(RepositoryFiles.PostgresInitDirectory).Order(StringComparer.Ordinal))
        {
            builder = builder.WithResourceMapping(new FileInfo(script), EmployeeDatabaseSettings.InitScriptsTargetDirectory);
        }

        return builder.Build();
    }

    private static async Task CreateDatabaseAsync(string superuserConnectionString, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(superuserConnectionString, cancellationToken);
        await using var command = new NpgsqlCommand(EmployeeDatabaseSettings.CreationScript, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static (string Write, string Read) BuildConnectionStrings(string superuserConnectionString, string appRolePassword)
    {
        var write = new NpgsqlConnectionStringBuilder(superuserConnectionString)
        {
            Database = EmployeeDatabaseSettings.DatabaseName,
            Username = EmployeeDatabaseSettings.AppRoleName,
            Password = appRolePassword,
            ApplicationName = "employee-it-write",
        };

        var read = new NpgsqlConnectionStringBuilder(write.ConnectionString)
        {
            ApplicationName = "employee-it-read",
            Options = ReadOnlyConnectionOptions,
        };

        return (write.ConnectionString, read.ConnectionString);
    }

    private static IConfiguration BuildConfiguration(string writeConnectionString, string? readConnectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [WriteConnectionKey] = writeConnectionString,
                [ReadConnectionKey] = readConnectionString,
            })
            .Build();

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static string NewSecret() => RandomNumberGenerator.GetString(SecretCharacters, SecretLength);

    private static InvalidOperationException NotInitialized() => new("EmployeeDatabaseFixture가 아직 준비되지 않았습니다(InitializeAsync 전).");

    private async Task<Respawner> CreateRespawnerAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenWriteConnectionAsync(cancellationToken);

        // 이름은 대소문자 그대로, 따옴표 없이 준다(소문자 · 따옴표 형태는 제외되지 않아 이력까지 비워짐, testing-strategy.md Q10).
        return await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = [PublicSchema],
            TablesToIgnore = [new Table(PublicSchema, MigrationHistoryTable)],
            WithReseed = false,
        });
    }
}
