namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Write Repository 구현의 기반 클래스입니다. 쓰기 DbContext(<see cref="WriteDbContextBase"/>)만 받습니다.
/// </summary>
/// <typeparam name="TContext">서비스 쓰기 DbContext.</typeparam>
/// <remarks>
/// <para>
/// 구현 클래스는 <c>internal sealed class EmployeeRepository(EmployeeDbContext db) : RepositoryBase&lt;EmployeeDbContext&gt;(db), IEmployeeRepository</c> 형태이고,
/// 메서드 하나 = 식 본문 LINQ 체인 하나만 둡니다(coding-conventions "Repository 규칙").
/// </para>
/// <para>
/// <see cref="Db"/>만 노출하고 <c>SaveChanges</c>를 감싸지 않습니다. 저장은 UnitOfWork가 합니다(ADR-0014).
/// 추상 클래스라 DI 자동 등록 검색에서 빠지고, 파생 구현이 마커(<c>IRepository</c>)를 상속한 인터페이스를 구현해 등록됩니다(ADR-0017).
/// </para>
/// </remarks>
public abstract class RepositoryBase<TContext>
    where TContext : WriteDbContextBase
{
    /// <summary>
    /// 쓰기 DbContext를 받아 Repository를 만듭니다.
    /// </summary>
    /// <param name="db">서비스 쓰기 DbContext.</param>
    /// <exception cref="ArgumentNullException"><paramref name="db"/>가 <see langword="null"/>인 경우.</exception>
    protected RepositoryBase(TContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        Db = db;
    }

    /// <summary>
    /// 서비스 쓰기 DbContext입니다.
    /// </summary>
    protected TContext Db { get; }
}
