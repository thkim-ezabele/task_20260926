using System.Reflection;
using EmergencyHub.BuildingBlocks.Domain.Events;
using EmergencyHub.BuildingBlocks.Domain.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;

/// <summary>
/// 쓰기 · 읽기 DbContext 기반이 함께 쓰는 EF Core 공통 모델 규칙의 단일 진입점입니다(database.md "EF Core 공통 모델 규칙").
/// </summary>
internal static class CommonModelConventions
{
    private const string XminColumnName = "xmin";
    private const string XminColumnType = "xid";

    /// <summary>
    /// 모델 생성 전 규칙: 도메인 이벤트 제외(TD-015), 강타입 ID 값 변환기 등록, <c>ck_</c> 생성 규칙(명명 규칙 뒤에 실행) 추가.
    /// </summary>
    /// <param name="configurationBuilder">규칙 구성 빌더.</param>
    /// <param name="definition">서비스 모델 정의.</param>
    public static void ConfigureConventions(ModelConfigurationBuilder configurationBuilder, IDbModelDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(definition);

        configurationBuilder.IgnoreAny<IDomainEvent>();

        foreach (var idType in FindStronglyTypedIdTypes(definition.StronglyTypedIdAssemblies))
        {
            configurationBuilder.Properties(idType).HaveConversion(typeof(StronglyTypedIdValueConverter<>).MakeGenericType(idType));
        }

        // 규칙 추가는 공급자 · 플러그인(EFCore.NamingConventions) 규칙 뒤에 붙으므로, 최종 snake_case 이름이 정해진 뒤 실행된다.
        configurationBuilder.Conventions.Add(_ => new EnumCheckConstraintConvention());
    }

    /// <summary>
    /// 서비스 매핑을 적용한 뒤 공통 뒤처리를 합니다: 강타입 ID 키 <c>ValueGeneratedNever</c>,
    /// owned가 아닌 최상위 엔티티 형식에 감사(<c>created_at</c> · <c>updated_at</c>) · 동시성(<c>xmin</c>) shadow property.
    /// </summary>
    /// <param name="modelBuilder">모델 빌더.</param>
    /// <param name="definition">서비스 모델 정의.</param>
    /// <remarks>
    /// 규칙 수준(Convention)이 아니라 명시(Explicit) 구성으로 두어 공급자 · 명명 규칙이 나중에 덮어쓰지 못하게 한다.
    /// <c>ValueGeneratedNever</c>는 <c>PropertiesConfigurationBuilder</c>로 걸 수 없어 여기서 키 속성에 직접 건다.
    /// </remarks>
    public static void BuildModel(ModelBuilder modelBuilder, IDbModelDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(definition);

        definition.ConfigureModel(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            MarkStronglyTypedIdKeysNeverGenerated(entityType);

            // TPH 파생 형식은 최상위 형식의 속성을 물려받는다. owned 형식은 소유자 행의 updated_at · xmin을 따른다.
            if (!entityType.IsOwned() && entityType.BaseType is null)
            {
                AddAuditProperties(entityType);
                AddConcurrencyToken(entityType);
            }
        }
    }

    /// <summary>
    /// 어셈블리에서 자기 자신을 형식 인자로 <see cref="IStronglyTypedId{TSelf}"/>를 구현한 값 형식을 찾습니다.
    /// </summary>
    /// <param name="assemblies">검색할 어셈블리. 중복은 한 번만 검색합니다.</param>
    /// <returns>강타입 ID 형식 목록.</returns>
    public static IReadOnlyList<Type> FindStronglyTypedIdTypes(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies.Distinct().SelectMany(assembly => assembly.GetTypes()).Where(IsStronglyTypedId).ToList();
    }

    private static bool IsStronglyTypedId(Type type) =>
        type.IsValueType
        && !type.IsGenericTypeDefinition
        && type.GetInterfaces().Any(contract =>
            contract.IsGenericType
            && contract.GetGenericTypeDefinition() == typeof(IStronglyTypedId<>)
            && contract.GetGenericArguments()[0] == type);

    private static void MarkStronglyTypedIdKeysNeverGenerated(IMutableEntityType entityType)
    {
        var keyProperties = entityType.FindPrimaryKey()?.Properties ?? [];

        foreach (var property in keyProperties.Where(property => IsStronglyTypedId(property.ClrType)))
        {
            property.ValueGenerated = ValueGenerated.Never;
        }
    }

    private static void AddAuditProperties(IMutableEntityType entityType)
    {
        foreach (var name in (string[])[ShadowPropertyNames.CreatedAt, ShadowPropertyNames.UpdatedAt])
        {
            entityType.AddProperty(name, typeof(DateTimeOffset)).IsNullable = false;
        }
    }

    // IsRowVersion()과 같은 구성(동시성 토큰 + OnAddOrUpdate)에 컬럼 이름 · 타입을 명시한다.
    // 명시하지 않으면 Npgsql 규칙과 snake_case 규칙의 적용 순서에 따라 일반 컬럼(version)이 생길 수 있다(database.md).
    private static void AddConcurrencyToken(IMutableEntityType entityType)
    {
        var version = entityType.AddProperty(ShadowPropertyNames.Version, typeof(uint));
        version.IsConcurrencyToken = true;
        version.ValueGenerated = ValueGenerated.OnAddOrUpdate;
        version.SetColumnName(XminColumnName);
        version.SetColumnType(XminColumnType);
    }
}
