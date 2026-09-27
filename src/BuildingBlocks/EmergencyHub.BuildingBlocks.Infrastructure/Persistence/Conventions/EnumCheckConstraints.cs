using System.Globalization;
using System.Text;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;

/// <summary>
/// enum 정의에서 <c>ck_</c> 체크 제약 이름과 SQL을 만드는 규칙입니다(database.md "코드값 규칙" · "비트 마스킹 규칙").
/// </summary>
/// <remarks>SQL은 손으로 쓰지 않고 enum 정의가 원본입니다. 값 · 비트를 추가하면 마이그레이션이 제약을 함께 바꿉니다.</remarks>
internal static class EnumCheckConstraints
{
    /// <summary>도우미가 속성에 남기는 표식(주석) 이름입니다.</summary>
    public const string AnnotationName = "EmergencyHub:EnumCheckConstraint";

    private const int MaxIdentifierBytes = 63;

    /// <summary>코드값 enum 규칙(기반 형식 <c>short</c>, <c>[Flags]</c> 아님, 0 말고 정의 값 1개 이상)을 확인합니다.</summary>
    /// <param name="enumType">enum 형식.</param>
    /// <exception cref="InvalidOperationException">규칙을 어긴 경우.</exception>
    public static void EnsureCodeEnum(Type enumType)
    {
        var underlying = Enum.GetUnderlyingType(enumType);
        if (underlying != typeof(short))
        {
            throw new InvalidOperationException(
                $"코드값 체크 제약 대상 enum {enumType.FullName}의 기반 형식은 short여야 합니다(현재 {underlying.Name}, database.md 코드값 규칙).");
        }

        if (IsFlags(enumType))
        {
            throw new InvalidOperationException(
                $"{enumType.FullName}은(는) [Flags] enum이라 조합 값을 허용하는 HasFlagsCheckConstraint를 써야 합니다.");
        }

        if (CodeValues(enumType).Count == 0)
        {
            throw new InvalidOperationException(
                $"코드값 enum {enumType.FullName}에 예약 값 0 말고 정의된 값이 없어 허용 목록이 비었습니다.");
        }
    }

    /// <summary>비트 플래그 enum 규칙(<c>[Flags]</c>, 기반 형식 <c>int</c> / <c>long</c>, 음수 값 없음, 정의 비트 1개 이상)을 확인합니다.</summary>
    /// <param name="enumType">enum 형식.</param>
    /// <exception cref="InvalidOperationException">규칙을 어긴 경우.</exception>
    public static void EnsureFlagsEnum(Type enumType)
    {
        if (!IsFlags(enumType))
        {
            throw new InvalidOperationException(
                $"{enumType.FullName}에 [Flags] 특성이 없습니다. 비트 플래그 체크 제약은 [Flags] enum만 받고, 일반 코드는 HasCodeCheckConstraint를 씁니다.");
        }

        var underlying = Enum.GetUnderlyingType(enumType);
        if (underlying != typeof(int) && underlying != typeof(long))
        {
            throw new InvalidOperationException(
                $"비트 플래그 체크 제약 대상 enum {enumType.FullName}의 기반 형식은 int 또는 long이어야 합니다(현재 {underlying.Name}, database.md 비트 마스킹 규칙).");
        }

        var values = DefinedValues(enumType);
        if (values.Any(value => value < 0))
        {
            throw new InvalidOperationException(
                $"[Flags] enum {enumType.FullName}에 음수 값(부호 비트)이 있습니다. 저장 값이 음수가 되어 col >= 0 조건과 맞지 않으므로 int는 31개, long은 63개 비트까지만 씁니다.");
        }

        if (Mask(enumType) == 0)
        {
            throw new InvalidOperationException($"[Flags] enum {enumType.FullName}에 정의된 비트가 없습니다(마스크 0).");
        }
    }

    /// <summary>코드값 체크 SQL <c>col IN (값, ...)</c>을 만듭니다(0 제외, 중복 없이 오름차순).</summary>
    /// <param name="column">최종(snake_case) 컬럼 이름.</param>
    /// <param name="enumType">코드값 enum 형식.</param>
    /// <returns>체크 제약 SQL.</returns>
    public static string CodeSql(string column, Type enumType) =>
        $"{column} IN ({string.Join(", ", CodeValues(enumType).Select(value => value.ToString(CultureInfo.InvariantCulture)))})";

    /// <summary>비트 플래그 체크 SQL <c>col &gt;= 0 AND (col &amp; ~mask) = 0</c>을 만듭니다(mask = 정의 값 전체 OR).</summary>
    /// <param name="column">최종(snake_case) 컬럼 이름.</param>
    /// <param name="enumType">[Flags] enum 형식.</param>
    /// <returns>체크 제약 SQL.</returns>
    /// <remarks>범위 조건(<c>&lt;= mask</c>)은 비트 자리에 빈 곳이 있으면 정의되지 않은 값을 통과시키므로 쓰지 않는다. 괄호는 연산자 우선순위 때문에 필수다.</remarks>
    public static string FlagsSql(string column, Type enumType) =>
        $"{column} >= 0 AND ({column} & ~{Mask(enumType).ToString(CultureInfo.InvariantCulture)}) = 0";

    /// <summary>제약 이름 <c>ck_&lt;table&gt;_&lt;column&gt;</c>을 만듭니다.</summary>
    /// <param name="table">최종 테이블 이름.</param>
    /// <param name="column">최종 컬럼 이름.</param>
    /// <returns>제약 이름.</returns>
    /// <exception cref="InvalidOperationException">이름이 63바이트를 넘는 경우(PostgreSQL이 경고 없이 자름).</exception>
    public static string Name(string table, string column)
    {
        var name = $"ck_{table}_{column}";
        if (Encoding.UTF8.GetByteCount(name) > MaxIdentifierBytes)
        {
            throw new InvalidOperationException(
                $"체크 제약 이름 {name}이(가) {MaxIdentifierBytes}바이트를 넘습니다. PostgreSQL은 긴 이름을 경고 없이 자르므로 테이블 · 컬럼 이름을 줄이세요(database.md).");
        }

        return name;
    }

    private static bool IsFlags(Type enumType) => enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

    private static List<long> DefinedValues(Type enumType) =>
        Enum.GetValuesAsUnderlyingType(enumType)
            .Cast<object>()
            .Select(value => Convert.ToInt64(value, CultureInfo.InvariantCulture))
            .ToList();

    private static List<long> CodeValues(Type enumType) =>
        DefinedValues(enumType).Where(value => value != 0).Distinct().Order().ToList();

    private static long Mask(Type enumType) => DefinedValues(enumType).Aggregate(0L, (mask, value) => mask | value);
}
