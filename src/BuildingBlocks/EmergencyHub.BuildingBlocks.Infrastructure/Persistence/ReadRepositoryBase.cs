namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Read Repository 구현의 기반 클래스입니다. 읽기 전용 DbContext(<see cref="ReadDbContextBase"/>)만 받습니다.
/// </summary>
/// <typeparam name="TContext">서비스 읽기 DbContext.</typeparam>
/// <remarks>
/// <para>
/// Query는 이 기반을 상속한 Read Repository가 <c>Select</c> 프로젝션으로 응답 <c>record</c>를 바로 만듭니다(ADR-0007).
/// 메서드 하나 = 식 본문 LINQ 체인 하나만 둡니다(coding-conventions "Repository 규칙").
/// </para>
/// <para>
/// <see cref="Db"/>만 노출합니다. 추상 클래스라 DI 자동 등록 검색에서 빠지고, 파생 구현이 마커(<c>IReadRepository</c>)를 상속한 인터페이스를 구현해 등록됩니다(ADR-0017).
/// </para>
/// </remarks>
public abstract class ReadRepositoryBase<TContext>
    where TContext : ReadDbContextBase
{
    /// <summary>
    /// 읽기 DbContext를 받아 Read Repository를 만듭니다.
    /// </summary>
    /// <param name="db">서비스 읽기 DbContext.</param>
    /// <exception cref="ArgumentNullException"><paramref name="db"/>가 <see langword="null"/>인 경우.</exception>
    protected ReadRepositoryBase(TContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        Db = db;
    }

    /// <summary>
    /// 서비스 읽기 전용 DbContext입니다.
    /// </summary>
    protected TContext Db { get; }
}
