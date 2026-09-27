using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

/// <summary>
/// 엔티티 매핑 도우미입니다(database.md "EF Core 공통 모델 규칙").
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// 유니크 인덱스를 만들고 이름을 <paramref name="name"/>으로 덮어씁니다. <c>EFCore.NamingConventions</c>가 붙이는 <c>ix_</c> 대신 <c>ux_</c>가 됩니다.
    /// </summary>
    /// <typeparam name="TEntity">엔티티 형식.</typeparam>
    /// <param name="builder">엔티티 형식 빌더.</param>
    /// <param name="indexExpression">인덱스 컬럼(여러 개면 익명 형식, 순서 유지).</param>
    /// <param name="name">서비스가 소유한 이름 상수.</param>
    /// <returns>인덱스 빌더.</returns>
    /// <exception cref="ArgumentNullException">인자가 <see langword="null"/>인 경우.</exception>
    public static IndexBuilder<TEntity> HasUniqueIndex<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> indexExpression,
        UniqueIndexName name)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(indexExpression);
        ArgumentNullException.ThrowIfNull(name);

        return builder.HasIndex(indexExpression).IsUnique().HasDatabaseName(name.Value);
    }
}
