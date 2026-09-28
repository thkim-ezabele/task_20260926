using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// 모든 서비스 DbContext가 같은 공급자 · 실행 전략 · 명명 규칙을 쓰도록 하는 옵션 구성의 단일 진입점입니다(database.md "EF Core 구성", ADR-0011).
/// </summary>
/// <remarks>
/// 등록 확장(<c>AddWriteDbContext</c> · <c>AddReadDbContext</c>) · 설계 시점 팩터리(<c>IDesignTimeDbContextFactory</c>) · 모델 메타데이터 테스트가
/// 이 메서드를 같이 써서 관계형 모델이 같게 만듭니다. 감사 인터셉터는 쓰기 등록만 붙이므로 여기에 넣지 않습니다.
/// 실행 전략(재시도 설정)은 여기 한 곳에서만 정합니다. 등록 확장의 추가 옵션 콜백에서 <c>UseNpgsql</c>을 다시 부르지 않습니다(BL-073).
/// EF 실패 로그 수준도 여기 한 곳에서 정합니다(BL-023, <see cref="FailureLogLevel"/>).
/// </remarks>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// EF Core 실패 이벤트 3개(<c>RelationalEventId.CommandError</c> · <c>CoreEventId.SaveChangesFailed</c> · <c>RelationalEventId.TransactionError</c>)의 로그 수준입니다(BL-023).
    /// </summary>
    /// <remarks>
    /// EF 기본값은 Error라, UnitOfWork가 <c>Result</c>로 바꾸는 정상 경합(23505 → 서비스 코드, 한 건당 Error 2건)과 실행 전략이 재시도로 회복한
    /// 일시 오류(커밋 시점 40001 → TransactionError 1건)에도 Error가 남았습니다(S03-T06 실측). 예외 로그는 경계에서 한 번이라는 규칙(logging-observability.md)에 따라
    /// 낮추고, 변환되지 않는 예외(23514 · 25006 등)는 전역 예외 처리기가 이벤트 1(Error)로, 재시도 한도 초과는 분류기 경로 301(Warning)로, MigrationService는 Worker가 한 번 남깁니다.
    /// 연결 실패(<c>ConnectionError</c>)와 재시도 경고(<c>ExecutionStrategyRetrying</c>)는 그대로 둡니다.
    /// </remarks>
    internal const LogLevel FailureLogLevel = LogLevel.Debug;

    /// <summary>
    /// Npgsql 공급자(<c>EnableRetryOnFailure</c>), snake_case 명명 규칙, EF 실패 로그 수준(<see cref="FailureLogLevel"/>)을 구성합니다.
    /// </summary>
    /// <param name="builder">DbContext 옵션 빌더.</param>
    /// <param name="connectionString">연결 문자열. 값은 예외 메시지에 넣지 않습니다(비밀번호).</param>
    /// <param name="retry">재시도 설정. <see langword="null"/>이면 Npgsql 기본값(최대 6회 · 최대 지연 30초)입니다.</param>
    /// <returns>같은 <paramref name="builder"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> 또는 <paramref name="connectionString"/>이 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/>이 비어 있거나 공백뿐인 경우.</exception>
    public static DbContextOptionsBuilder UseBuildingBlocksNpgsql(
        this DbContextOptionsBuilder builder,
        string connectionString,
        DbRetryOptions? retry = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder
            .UseNpgsql(connectionString, npgsql =>
            {
                if (retry is null)
                {
                    npgsql.EnableRetryOnFailure();
                }
                else
                {
                    npgsql.EnableRetryOnFailure(retry.MaxRetryCount, retry.MaxRetryDelay, errorCodesToAdd: null);
                }
            })
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Log(
                (RelationalEventId.CommandError, FailureLogLevel),
                (CoreEventId.SaveChangesFailed, FailureLogLevel),
                (RelationalEventId.TransactionError, FailureLogLevel)));
    }

    /// <summary>
    /// <see cref="UseBuildingBlocksNpgsql(DbContextOptionsBuilder, string, DbRetryOptions?)"/>의 형식 있는 빌더 버전입니다.
    /// </summary>
    /// <typeparam name="TContext">DbContext 형식.</typeparam>
    /// <param name="builder">DbContext 옵션 빌더.</param>
    /// <param name="connectionString">연결 문자열.</param>
    /// <param name="retry">재시도 설정. <see langword="null"/>이면 Npgsql 기본값입니다.</param>
    /// <returns>같은 <paramref name="builder"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> 또는 <paramref name="connectionString"/>이 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/>이 비어 있거나 공백뿐인 경우.</exception>
    public static DbContextOptionsBuilder<TContext> UseBuildingBlocksNpgsql<TContext>(
        this DbContextOptionsBuilder<TContext> builder,
        string connectionString,
        DbRetryOptions? retry = null)
        where TContext : DbContext
    {
        ((DbContextOptionsBuilder)builder).UseBuildingBlocksNpgsql(connectionString, retry);
        return builder;
    }
}
