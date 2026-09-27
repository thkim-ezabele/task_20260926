using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// 모든 서비스 DbContext가 같은 공급자 · 실행 전략 · 명명 규칙을 쓰도록 하는 옵션 구성의 단일 진입점입니다(database.md "EF Core 구성", ADR-0011).
/// </summary>
/// <remarks>
/// 등록 확장(<c>AddWriteDbContext</c> · <c>AddReadDbContext</c>) · 설계 시점 팩터리(<c>IDesignTimeDbContextFactory</c>) · 모델 메타데이터 테스트가
/// 이 메서드를 같이 써서 관계형 모델이 같게 만듭니다. 감사 인터셉터는 쓰기 등록만 붙이므로 여기에 넣지 않습니다.
/// </remarks>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Npgsql 공급자(<c>EnableRetryOnFailure</c>, Npgsql 기본값: 최대 6회 · 최대 지연 30초)와 snake_case 명명 규칙을 구성합니다.
    /// </summary>
    /// <param name="builder">DbContext 옵션 빌더.</param>
    /// <param name="connectionString">연결 문자열. 값은 예외 메시지에 넣지 않습니다(비밀번호).</param>
    /// <returns>같은 <paramref name="builder"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> 또는 <paramref name="connectionString"/>이 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/>이 비어 있거나 공백뿐인 경우.</exception>
    public static DbContextOptionsBuilder UseBuildingBlocksNpgsql(this DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention();
    }

    /// <summary>
    /// <see cref="UseBuildingBlocksNpgsql(DbContextOptionsBuilder, string)"/>의 형식 있는 빌더 버전입니다.
    /// </summary>
    /// <typeparam name="TContext">DbContext 형식.</typeparam>
    /// <param name="builder">DbContext 옵션 빌더.</param>
    /// <param name="connectionString">연결 문자열.</param>
    /// <returns>같은 <paramref name="builder"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> 또는 <paramref name="connectionString"/>이 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/>이 비어 있거나 공백뿐인 경우.</exception>
    public static DbContextOptionsBuilder<TContext> UseBuildingBlocksNpgsql<TContext>(this DbContextOptionsBuilder<TContext> builder, string connectionString)
        where TContext : DbContext
    {
        ((DbContextOptionsBuilder)builder).UseBuildingBlocksNpgsql(connectionString);
        return builder;
    }
}
