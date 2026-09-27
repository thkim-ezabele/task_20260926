using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Auditing;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// 공통 DbContext 등록과 UnitOfWork 등록입니다(database.md "EF Core 구성 · 공통 DbContext 등록", ADR-0011, ADR-0014).
/// </summary>
/// <remarks>
/// Api는 쓰기 · 읽기 · UnitOfWork를 모두, MigrationService는 쓰기만 등록합니다. 순서 예:
/// <c>AddBuildingBlocksInfrastructure()</c> → <c>AddConventionalServices(...)</c> → <c>AddWriteDbContext</c> → <c>AddReadDbContext</c> → <c>AddUnitOfWork</c>.
/// DbContext는 <c>AddDbContext</c>(Scoped)로 등록하고 풀링 · Aspire 클라이언트 통합은 쓰지 않습니다(ADR-0011).
/// </remarks>
public static class PersistenceServiceCollectionExtensions
{
    private const string WriteConnectionKey = "ConnectionStrings:Write";
    private const string ReadConnectionKey = "ConnectionStrings:Read";

    /// <summary>
    /// 서비스 쓰기 DbContext를 Scoped로 등록합니다. 공통 옵션(<see cref="DbContextOptionsBuilderExtensions.UseBuildingBlocksNpgsql(DbContextOptionsBuilder, string, DbRetryOptions?)"/>)에
    /// 감사 인터셉터(Singleton 한 인스턴스)를 붙입니다.
    /// </summary>
    /// <typeparam name="TContext">서비스 쓰기 DbContext.</typeparam>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="connectionString">쓰기 연결 문자열(<c>ConnectionStrings:Write</c>).</param>
    /// <param name="configure">공통 옵션 뒤에 적용할 추가 옵션(예: Development에서만 켜는 옵션). 없으면 <see langword="null"/>.</param>
    /// <param name="retry">재시도 설정(공통 옵션 구성에 그대로 넘김). <see langword="null"/>이면 Npgsql 기본값입니다.</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="InvalidOperationException">연결 문자열이 없거나 비어 있는 경우(시작 시 실패, 값은 메시지에 넣지 않음).</exception>
    public static IServiceCollection AddWriteDbContext<TContext>(
        this IServiceCollection services,
        string? connectionString,
        Action<DbContextOptionsBuilder>? configure = null,
        DbRetryOptions? retry = null)
        where TContext : WriteDbContextBase
    {
        ArgumentNullException.ThrowIfNull(services);
        EnsureConnectionString(connectionString, WriteConnectionKey, typeof(TContext));

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(provider => new AuditSaveChangesInterceptor(provider.GetRequiredService<TimeProvider>()));
        services.AddDbContext<TContext>((provider, options) =>
        {
            options.UseBuildingBlocksNpgsql(connectionString, retry).AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>());
            configure?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// 서비스 읽기 DbContext를 Scoped로 등록합니다. 공통 옵션만 쓰고 감사 인터셉터는 붙이지 않습니다(추적 기본값 NoTracking은 <see cref="ReadDbContextBase"/>가 설정).
    /// </summary>
    /// <typeparam name="TContext">서비스 읽기 DbContext.</typeparam>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="connectionString">읽기 연결 문자열(<c>ConnectionStrings:Read</c>).</param>
    /// <param name="configure">공통 옵션 뒤에 적용할 추가 옵션. 없으면 <see langword="null"/>.</param>
    /// <param name="retry">재시도 설정(공통 옵션 구성에 그대로 넘김). <see langword="null"/>이면 Npgsql 기본값입니다.</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="InvalidOperationException">연결 문자열이 없거나 비어 있는 경우(시작 시 실패, 값은 메시지에 넣지 않음).</exception>
    public static IServiceCollection AddReadDbContext<TContext>(
        this IServiceCollection services,
        string? connectionString,
        Action<DbContextOptionsBuilder>? configure = null,
        DbRetryOptions? retry = null)
        where TContext : ReadDbContextBase
    {
        ArgumentNullException.ThrowIfNull(services);
        EnsureConnectionString(connectionString, ReadConnectionKey, typeof(TContext));

        services.AddDbContext<TContext>(options =>
        {
            options.UseBuildingBlocksNpgsql(connectionString, retry);
            configure?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// <see cref="IUnitOfWork"/>를 서비스 쓰기 DbContext에 묶어 Scoped로 등록하고, 23505 매핑 레지스트리를 Singleton으로 등록합니다.
    /// </summary>
    /// <typeparam name="TContext">서비스 쓰기 DbContext(<see cref="AddWriteDbContext{TContext}"/>로 등록한 형식).</typeparam>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="configureUniqueConstraintErrors">자기 서비스 유니크 인덱스의 오류 매핑. 없으면 모든 23505가 3003입니다.</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="IUnitOfWork"/>가 이미 등록된 경우(다른 <typeparamref name="TContext"/>로 다시 부른 경우 포함. 서비스 하나에 쓰기 DbContext는 하나),
    /// 또는 매핑에서 같은 인덱스를 두 번 등록한 경우.
    /// </exception>
    /// <exception cref="ArgumentException">매핑 오류의 유형이 Conflict가 아닌 경우.</exception>
    /// <remarks>
    /// UnitOfWork와 Write Repository는 같은 스코프의 같은 <typeparamref name="TContext"/> 인스턴스를 씁니다.
    /// <see cref="IPreCommitHook"/> 구현이 등록되어 있으면 커밋 직전에 순서대로 부릅니다(지금은 없음).
    /// </remarks>
    public static IServiceCollection AddUnitOfWork<TContext>(
        this IServiceCollection services,
        Action<UniqueConstraintErrorsBuilder>? configureUniqueConstraintErrors = null)
        where TContext : WriteDbContextBase
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IUnitOfWork)) is { } existing)
        {
            throw new InvalidOperationException(
                $"IUnitOfWork가 이미 등록되어 있습니다(구현: {existing.ImplementationType?.Name ?? "팩터리 또는 인스턴스"}). "
                + "AddUnitOfWork는 서비스의 쓰기 DbContext 하나로 한 번만 호출합니다.");
        }

        var builder = new UniqueConstraintErrorsBuilder();
        configureUniqueConstraintErrors?.Invoke(builder);

        services.AddSingleton(builder.Build());
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();

        return services;
    }

    private static void EnsureConnectionString([NotNull] string? connectionString, string key, Type contextType)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{contextType.Name}의 연결 문자열({key})이 없거나 비어 있습니다. 설정 · 시크릿으로 주입하세요.");
        }
    }
}
