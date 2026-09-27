using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;

/// <summary>
/// 도우미 표식이 붙은 enum 속성마다 <c>ck_&lt;table&gt;_&lt;column&gt;</c> 체크 제약을 만드는 모델 확정 규칙입니다.
/// </summary>
/// <remarks>
/// 매핑 시점에는 테이블 · 컬럼 이름이 확정되지 않았을 수 있어(DbSet 이름 · snake_case 규칙은 모델 확정 때 적용),
/// 명명 규칙 뒤에 등록된 이 규칙이 메타데이터(<c>GetTableName</c>, <c>GetColumnName(StoreObjectIdentifier)</c>)로 최종 이름을 읽는다.
/// 만든 뒤 표식을 지워 마이그레이션 스냅숏에 남기지 않는다.
/// </remarks>
internal sealed class EnumCheckConstraintConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var targets = modelBuilder.Metadata.GetEntityTypes()
            .SelectMany(entityType => entityType.GetDeclaredProperties().Select(property =>
                (EntityType: entityType, Property: property, Kind: property.FindAnnotation(EnumCheckConstraints.AnnotationName)?.Value as EnumCheckConstraintKind?)))
            .Where(target => target.Kind is not null)
            .ToList();

        foreach (var (entityType, property, kind) in targets)
        {
            var table = entityType.GetTableName()
                ?? throw new InvalidOperationException($"{entityType.DisplayName()}이(가) 테이블에 매핑되지 않아 {property.Name}의 체크 제약을 만들 수 없습니다.");
            var column = property.GetColumnName(StoreObjectIdentifier.Table(table, entityType.GetSchema()))
                ?? throw new InvalidOperationException($"{entityType.DisplayName()}.{property.Name}이(가) 테이블 {table}의 컬럼에 매핑되지 않았습니다.");
            var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            var sql = kind == EnumCheckConstraintKind.Flags
                ? EnumCheckConstraints.FlagsSql(column, enumType)
                : EnumCheckConstraints.CodeSql(column, enumType);

            entityType.AddCheckConstraint(EnumCheckConstraints.Name(table, column), sql);
            property.RemoveAnnotation(EnumCheckConstraints.AnnotationName);
        }
    }
}
