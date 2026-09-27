using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

/// <summary>
/// enum 속성의 <c>ck_&lt;table&gt;_&lt;column&gt;</c> 체크 제약 도우미입니다(database.md "코드값 규칙" · "비트 마스킹 규칙").
/// </summary>
/// <remarks>
/// 도우미는 enum 기반 형식을 바로 검사하고(어기면 모델 생성 중 예외) 속성에 표식만 남깁니다.
/// 이름과 SQL은 명명 규칙이 최종 이름을 정한 뒤 공통 규칙이 enum 정의에서 만듭니다(손으로 SQL을 쓰지 않음).
/// </remarks>
public static class PropertyBuilderExtensions
{
    /// <summary>
    /// 코드값 체크 제약 <c>col IN (정의 값, 0 제외)</c>을 겁니다.
    /// </summary>
    /// <typeparam name="TEnum">기반 형식이 <c>short</c>인 코드 enum.</typeparam>
    /// <param name="builder">속성 빌더.</param>
    /// <returns>같은 속성 빌더.</returns>
    /// <exception cref="InvalidOperationException">기반 형식이 <c>short</c>가 아니거나, <c>[Flags]</c>이거나, 0 말고 정의 값이 없는 경우.</exception>
    public static PropertyBuilder<TEnum> HasCodeCheckConstraint<TEnum>(this PropertyBuilder<TEnum> builder)
        where TEnum : struct, Enum =>
        Mark(builder, typeof(TEnum), EnumCheckConstraintKind.Code);

    /// <summary>
    /// NULL 허용 코드값 속성에 체크 제약을 겁니다. NULL은 체크 제약을 통과하므로 <c>IS NULL</c>을 따로 넣지 않습니다.
    /// </summary>
    /// <typeparam name="TEnum">기반 형식이 <c>short</c>인 코드 enum.</typeparam>
    /// <param name="builder">속성 빌더.</param>
    /// <returns>같은 속성 빌더.</returns>
    /// <exception cref="InvalidOperationException"><see cref="HasCodeCheckConstraint{TEnum}(PropertyBuilder{TEnum})"/>과 같은 경우.</exception>
    public static PropertyBuilder<TEnum?> HasCodeCheckConstraint<TEnum>(this PropertyBuilder<TEnum?> builder)
        where TEnum : struct, Enum =>
        Mark(builder, typeof(TEnum), EnumCheckConstraintKind.Code);

    /// <summary>
    /// 비트 플래그 체크 제약 <c>col &gt;= 0 AND (col &amp; ~mask) = 0</c>을 겁니다(mask = 정의 값 전체 OR).
    /// </summary>
    /// <typeparam name="TEnum">기반 형식이 <c>int</c> / <c>long</c>인 <c>[Flags]</c> enum.</typeparam>
    /// <param name="builder">속성 빌더.</param>
    /// <returns>같은 속성 빌더.</returns>
    /// <exception cref="InvalidOperationException"><c>[Flags]</c>가 아니거나, 기반 형식이 <c>int</c> / <c>long</c>이 아니거나, 음수 값이 있거나, 정의 비트가 없는 경우.</exception>
    public static PropertyBuilder<TEnum> HasFlagsCheckConstraint<TEnum>(this PropertyBuilder<TEnum> builder)
        where TEnum : struct, Enum =>
        Mark(builder, typeof(TEnum), EnumCheckConstraintKind.Flags);

    /// <summary>
    /// NULL 허용 비트 플래그 속성에 체크 제약을 겁니다.
    /// </summary>
    /// <typeparam name="TEnum">기반 형식이 <c>int</c> / <c>long</c>인 <c>[Flags]</c> enum.</typeparam>
    /// <param name="builder">속성 빌더.</param>
    /// <returns>같은 속성 빌더.</returns>
    /// <exception cref="InvalidOperationException"><see cref="HasFlagsCheckConstraint{TEnum}(PropertyBuilder{TEnum})"/>과 같은 경우.</exception>
    public static PropertyBuilder<TEnum?> HasFlagsCheckConstraint<TEnum>(this PropertyBuilder<TEnum?> builder)
        where TEnum : struct, Enum =>
        Mark(builder, typeof(TEnum), EnumCheckConstraintKind.Flags);

    private static PropertyBuilder<TProperty> Mark<TProperty>(PropertyBuilder<TProperty> builder, Type enumType, EnumCheckConstraintKind kind)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (kind == EnumCheckConstraintKind.Flags)
        {
            EnumCheckConstraints.EnsureFlagsEnum(enumType);
        }
        else
        {
            EnumCheckConstraints.EnsureCodeEnum(enumType);
        }

        return builder.HasAnnotation(EnumCheckConstraints.AnnotationName, kind);
    }
}
