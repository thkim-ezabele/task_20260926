using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// 서비스 쓰기 DbContext(<c>&lt;Service&gt;DbContext</c>)의 기반 클래스입니다. Command · UnitOfWork · 마이그레이션이 이 형식을 씁니다(ADR-0009).
/// </summary>
/// <remarks>
/// <para>
/// 모델 구성은 <see cref="ModelDefinition"/>과 공통 규칙으로만 정해지도록 <c>ConfigureConventions</c> · <c>OnModelCreating</c>을 봉인합니다.
/// <see cref="ReadDbContextBase"/>도 같은 진입점을 호출하므로 두 모델이 같습니다.
/// </para>
/// <para>
/// 연결 · snake_case 명명 규칙 · 감사 인터셉터 · 실행 전략은 공통 등록 확장이 붙입니다(S02-T07).
/// Handler · Repository는 <c>SaveChanges</c>를 부르지 않고 UnitOfWork가 저장합니다(ADR-0014).
/// </para>
/// </remarks>
public abstract class WriteDbContextBase : DbContext
{
    /// <summary>
    /// 옵션을 받아 쓰기 DbContext를 만듭니다.
    /// </summary>
    /// <param name="options">DbContext 옵션.</param>
    protected WriteDbContextBase(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// 이 DbContext의 모델 정의입니다. 읽기 DbContext와 같은 인스턴스를 돌려줍니다.
    /// </summary>
    protected abstract IDbModelDefinition ModelDefinition { get; }

    /// <inheritdoc/>
    protected sealed override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        CommonModelConventions.ConfigureConventions(configurationBuilder, ModelDefinition);

    /// <inheritdoc/>
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder) =>
        CommonModelConventions.BuildModel(modelBuilder, ModelDefinition);
}
