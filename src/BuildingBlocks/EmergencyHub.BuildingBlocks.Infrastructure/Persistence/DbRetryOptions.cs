namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// 실행 전략(<c>EnableRetryOnFailure</c>)의 재시도 설정입니다(database.md "공통 DbContext 등록", BL-073).
/// </summary>
/// <remarks>
/// <para>
/// 공통 옵션 구성(<see cref="DbContextOptionsBuilderExtensions.UseBuildingBlocksNpgsql(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder, string, DbRetryOptions?)"/>)의
/// 선택 인자로만 씁니다. 넘기지 않으면 Npgsql 기본값(최대 6회, 최대 지연 30초)입니다.
/// 값(BL-073 확정, S03-T06 실측): Api는 3회 · 5초(Api 요청 제한 시간 없음), MigrationService는 기본값입니다.
/// </para>
/// <para>
/// 생성자에서 값을 검사하고 속성은 읽기 전용이라 <c>with</c> 식으로 검사를 우회할 수 없습니다. 0회 · 0초는 "재시도하지 않음"으로 허용합니다.
/// </para>
/// </remarks>
public sealed record DbRetryOptions
{
    /// <summary>
    /// 재시도 설정을 만듭니다.
    /// </summary>
    /// <param name="maxRetryCount">최대 재시도 횟수(0 이상).</param>
    /// <param name="maxRetryDelay">재시도 사이 최대 지연(0 이상).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetryCount"/> 또는 <paramref name="maxRetryDelay"/>가 음수인 경우.</exception>
    public DbRetryOptions(int maxRetryCount, TimeSpan maxRetryDelay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetryCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetryDelay, TimeSpan.Zero);

        MaxRetryCount = maxRetryCount;
        MaxRetryDelay = maxRetryDelay;
    }

    /// <summary>최대 재시도 횟수입니다.</summary>
    public int MaxRetryCount { get; }

    /// <summary>재시도 사이 최대 지연입니다.</summary>
    public TimeSpan MaxRetryDelay { get; }
}
