using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// 서비스 읽기 전용 DbContext(<c>&lt;Service&gt;ReadDbContext</c>)의 기반 클래스입니다. Read Repository만 이 형식을 씁니다(ADR-0007, ADR-0009).
/// </summary>
/// <remarks>
/// <para>추적 기본값은 <see cref="QueryTrackingBehavior.NoTracking"/>이고, <c>SaveChanges</c> 오버로드 4개를 모두 봉인해 <see cref="InvalidOperationException"/>을 던집니다.</para>
/// <para>
/// 모델은 <see cref="WriteDbContextBase"/>와 같은 공통 규칙 · 같은 <see cref="IDbModelDefinition"/>으로 만듭니다. 마이그레이션 대상이 아니고 감사 인터셉터도 붙이지 않습니다.
/// 읽기 연결의 <c>default_transaction_read_only=on</c>은 DB 쪽 안전장치입니다(database.md).
/// </para>
/// </remarks>
public abstract class ReadDbContextBase : DbContext
{
    /// <summary>
    /// 옵션을 받아 읽기 전용 DbContext를 만들고 추적 기본값을 <see cref="QueryTrackingBehavior.NoTracking"/>으로 둡니다.
    /// </summary>
    /// <param name="options">DbContext 옵션.</param>
    protected ReadDbContextBase(DbContextOptions options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    /// <summary>
    /// 이 DbContext의 모델 정의입니다. 쓰기 DbContext와 같은 인스턴스를 돌려줍니다.
    /// </summary>
    protected abstract IDbModelDefinition ModelDefinition { get; }

    /// <summary>읽기 전용이라 항상 예외를 던집니다.</summary>
    /// <returns>반환하지 않습니다.</returns>
    /// <exception cref="InvalidOperationException">항상.</exception>
    public sealed override int SaveChanges() => throw SavingNotAllowed();

    /// <summary>읽기 전용이라 항상 예외를 던집니다.</summary>
    /// <param name="acceptAllChangesOnSuccess">쓰이지 않습니다.</param>
    /// <returns>반환하지 않습니다.</returns>
    /// <exception cref="InvalidOperationException">항상.</exception>
    public sealed override int SaveChanges(bool acceptAllChangesOnSuccess) => throw SavingNotAllowed();

    /// <summary>읽기 전용이라 항상 예외를 던집니다(작업을 반환하지 않고 호출 즉시 던짐).</summary>
    /// <param name="cancellationToken">쓰이지 않습니다.</param>
    /// <returns>반환하지 않습니다.</returns>
    /// <exception cref="InvalidOperationException">항상.</exception>
    public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw SavingNotAllowed();

    /// <summary>읽기 전용이라 항상 예외를 던집니다(작업을 반환하지 않고 호출 즉시 던짐).</summary>
    /// <param name="acceptAllChangesOnSuccess">쓰이지 않습니다.</param>
    /// <param name="cancellationToken">쓰이지 않습니다.</param>
    /// <returns>반환하지 않습니다.</returns>
    /// <exception cref="InvalidOperationException">항상.</exception>
    public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw SavingNotAllowed();

    /// <inheritdoc/>
    protected sealed override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        CommonModelConventions.ConfigureConventions(configurationBuilder, ModelDefinition);

    /// <inheritdoc/>
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder) =>
        CommonModelConventions.BuildModel(modelBuilder, ModelDefinition);

    private InvalidOperationException SavingNotAllowed() =>
        new($"{GetType().Name}은(는) 읽기 전용 DbContext라 저장할 수 없습니다. 상태 변경은 Command가 쓰기 DbContext와 IUnitOfWork로 합니다(ADR-0009, ADR-0014).");
}
