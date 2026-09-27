using System.Data.Common;
using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.Employee.MigrationService;

/// <summary>
/// 새 DI 스코프의 쓰기 DbContext로 실행 전략 안에서 <c>MigrateAsync</c>만 하고, 종료 코드를 정한 뒤 호스트를 멈춥니다
/// (database.md "MigrationService 동작 사양", ADR-0012).
/// </summary>
/// <remarks>
/// 예외 · 취소는 삼키고 종료 코드와 Error 로그 한 건으로 알립니다(BackgroundService의 StopHost 동작에 맡기지 않음).
/// <c>EnsureCreated</c> · <c>GetPendingMigrations</c> 분기 · 원시 SQL · 시드는 없습니다.
/// </remarks>
internal sealed class MigrationWorker : BackgroundService
{
    private static readonly string DbContextTypeName = nameof(EmployeeDbContext);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MigrationWorker> _logger;
    private readonly Func<IServiceProvider, CancellationToken, Task> _migrate;
    private readonly Action<int> _setExitCode;

    /// <summary>DI가 쓰는 생성자입니다. 운영 적용 동작과 <see cref="Environment.ExitCode"/>를 씁니다.</summary>
    public MigrationWorker(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        TimeProvider timeProvider,
        ILogger<MigrationWorker> logger)
        : this(scopeFactory, lifetime, timeProvider, logger, ApplyMigrationsAsync, static exitCode => Environment.ExitCode = exitCode)
    {
    }

    /// <summary>
    /// 적용 동작과 종료 코드 기록을 바꿀 수 있는 생성자입니다(DB 없는 단위 테스트, <see cref="Environment.ExitCode"/>는 프로세스 전역).
    /// DI는 public 생성자만 쓰므로 이 생성자를 고르지 않습니다.
    /// </summary>
    internal MigrationWorker(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        TimeProvider timeProvider,
        ILogger<MigrationWorker> logger,
        Func<IServiceProvider, CancellationToken, Task> migrate,
        Action<int> setExitCode)
    {
        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
        _timeProvider = timeProvider;
        _logger = logger;
        _migrate = migrate;
        _setExitCode = setExitCode;
    }

    /// <summary>마이그레이션을 적용하고 종료 코드를 돌려줍니다. 예외는 밖으로 내보내지 않습니다.</summary>
    /// <param name="cancellationToken">호스트 종료 토큰.</param>
    /// <returns>종료 코드.</returns>
    internal async Task<MigrationExitCode> RunAsync(CancellationToken cancellationToken)
    {
        var startedAt = _timeProvider.GetTimestamp();

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await _migrate(scope.ServiceProvider, cancellationToken);
        }
        catch (Exception exception)
        {
            // 작업 최상위 경계: 로그 한 건 + 종료 코드로 처리한다(coding-conventions "예외 처리 규칙", logging-observability "예외는 경계에서 한 번").
            var exitCode = exception is OperationCanceledException ? MigrationExitCode.Canceled : MigrationExitCode.Failed;
            _logger.MigrationsFailed(
                exception,
                DbContextTypeName,
                exception.GetType().FullName ?? exception.GetType().Name,
                FindSqlState(exception),
                ElapsedMilliseconds(startedAt),
                (int)exitCode);
            return exitCode;
        }

        _logger.MigrationsApplied(DbContextTypeName, ElapsedMilliseconds(startedAt));
        return MigrationExitCode.Succeeded;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var exitCode = await RunAsync(stoppingToken);

        _setExitCode((int)exitCode);
        _lifetime.StopApplication();
    }

    // 운영 경로: 실행 전략 재시도는 MigrateAsync 전체를 다시 부른다(마이그레이션마다 트랜잭션 + 이력 테이블로 적용분을 건너뜀, database.md).
    private static async Task ApplyMigrationsAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<EmployeeDbContext>();
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(token => db.Database.MigrateAsync(token), cancellationToken);
    }

    // 실행 전략이 재시도 한도를 넘기면 원인을 안쪽 예외로 감싸므로 사슬 전체에서 찾는다. Npgsql 형식 대신 DbException.SqlState를 쓴다.
    private static string? FindSqlState(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException { SqlState: { } sqlState })
            {
                return sqlState;
            }
        }

        return null;
    }

    private double ElapsedMilliseconds(long startedAt) => _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
}
