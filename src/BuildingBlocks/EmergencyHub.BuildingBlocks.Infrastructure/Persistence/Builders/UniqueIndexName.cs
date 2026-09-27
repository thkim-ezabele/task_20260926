using System.Text;
using System.Text.RegularExpressions;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

/// <summary>
/// 유니크 인덱스 이름 <c>ux_&lt;table&gt;_&lt;columns&gt;</c>입니다. 서비스 Infrastructure의 이름 상수 한 곳에 두고,
/// 매핑(<see cref="EntityTypeBuilderExtensions.HasUniqueIndex{TEntity}"/>)과 23505 매핑 레지스트리가 같은 값을 참조합니다(database.md).
/// </summary>
/// <remarks>
/// 형식을 만들 때 규칙을 검사합니다: <c>ux_</c> 접두사, 소문자 snake_case(<c>[a-z0-9_]</c>), 63바이트 이하.
/// PostgreSQL은 긴 이름을 경고 없이 자르므로 잘린 이름은 23505 <c>ConstraintName</c>과 일치하지 않습니다.
/// </remarks>
public sealed partial record UniqueIndexName
{
    /// <summary>PostgreSQL 식별자 최대 길이(바이트)입니다.</summary>
    public const int MaxBytes = 63;

    /// <summary>
    /// 이름을 검사해 만듭니다.
    /// </summary>
    /// <param name="value">인덱스 이름(예: <c>ux_employees_email</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException">형식이 규칙과 다르거나 63바이트를 넘는 경우.</exception>
    public UniqueIndexName(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!Pattern().IsMatch(value))
        {
            throw new ArgumentException($"유니크 인덱스 이름은 ux_로 시작하는 소문자 snake_case여야 합니다: '{value}'", nameof(value));
        }

        if (Encoding.UTF8.GetByteCount(value) > MaxBytes)
        {
            throw new ArgumentException($"유니크 인덱스 이름이 {MaxBytes}바이트를 넘습니다(PostgreSQL이 잘라 저장함): '{value}'", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// 인덱스 이름입니다.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"\Aux_[a-z0-9_]+\z", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
