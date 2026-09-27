using System.Globalization;

namespace EmergencyHub.BuildingBlocks.Application.Validation;

/// <summary>
/// 코드값 enum의 정의 여부 판정입니다(ADR-0008, ADR-0018 "정의되지 않은 코드값"). 형식마다 한 번 계산해 캐시합니다.
/// </summary>
/// <typeparam name="TEnum">코드값 enum 형식.</typeparam>
/// <remarks>
/// <para>일반 enum: <see cref="Enum.IsDefined{TEnum}(TEnum)"/>이고, 0은 정의되어 있어도 예약 값(<c>None</c> / <c>Unknown</c>)이라 거부합니다.</para>
/// <para>
/// <c>[Flags]</c> enum: 정의된 멤버 값을 모두 OR한 마스크 밖의 비트가 없으면 허용합니다. 0(빈 조합)은 허용하고,
/// "최소 하나" 같은 조합 규칙은 Aggregate / Value Object가 검증합니다(coding-conventions "비트 마스킹").
/// 비트는 부호 확장한 64비트 값으로 비교하므로 <c>long</c>의 부호 비트(1 &lt;&lt; 63)도 다른 비트와 같게 다룹니다.
/// </para>
/// <para>FluentValidation의 <c>IsInEnum()</c>은 일반 enum의 0을 허용하므로 쓰지 않고 이 판정을 씁니다.</para>
/// </remarks>
internal static class DefinedEnumValues<TEnum>
    where TEnum : struct, Enum
{
    private static readonly bool IsFlags = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);

    private static readonly long DefinedMask = Enum.GetValues<TEnum>().Aggregate(0L, (mask, value) => mask | ToInt64(value));

    /// <summary>값이 정의된 코드(일반 enum) 또는 정의된 비트의 조합(<c>[Flags]</c>)이면 <see langword="true"/>입니다.</summary>
    /// <param name="value">검사할 값.</param>
    /// <returns>정의 여부.</returns>
    public static bool IsDefined(TEnum value) =>
        IsFlags
            ? (ToInt64(value) & ~DefinedMask) == 0
            : ToInt64(value) != 0 && Enum.IsDefined(value);

    private static long ToInt64(TEnum value) => Convert.ToInt64(value, CultureInfo.InvariantCulture);
}
