using System.Collections.Frozen;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;

/// <summary>
/// 23505(유니크 위반)의 <c>ConstraintName</c> → 서비스 <see cref="Error"/> 매핑입니다. 등록 뒤 바뀌지 않으며 Singleton으로 등록됩니다
/// (database.md "23505 매핑 레지스트리 계약").
/// </summary>
/// <remarks>
/// <see cref="UniqueConstraintErrorsBuilder"/>로만 만듭니다. 조회는 <c>ConstraintName</c> 문자열과 <see cref="UniqueIndexName.Value"/>의 Ordinal 비교이고,
/// 조회할 때 <see cref="UniqueIndexName"/>을 만들지 않습니다(<c>pk_</c> 등 <c>ux_</c>가 아닌 이름은 생성자가 예외를 던지므로).
/// </remarks>
public sealed class UniqueConstraintErrorRegistry
{
    private readonly FrozenDictionary<string, Error> _errors;

    internal UniqueConstraintErrorRegistry(IReadOnlyDictionary<UniqueIndexName, Error> mappings)
    {
        _errors = mappings.ToFrozenDictionary(pair => pair.Key.Value, pair => pair.Value, StringComparer.Ordinal);
        IndexNames = [.. mappings.Keys];
    }

    /// <summary>
    /// 매핑된 유니크 인덱스 이름입니다. 서비스 테스트는 이 이름이 모두 모델의 유니크 인덱스 이름(<c>GetDatabaseName()</c>)에 있는지 단언합니다.
    /// </summary>
    public IReadOnlyCollection<UniqueIndexName> IndexNames { get; }

    /// <summary>
    /// 제약 이름에 매핑된 오류를 찾습니다.
    /// </summary>
    /// <param name="constraintName">PostgreSQL이 보고한 제약(인덱스) 이름. <see langword="null"/>이거나 비어 있을 수 있습니다.</param>
    /// <returns>매핑된 오류. 없으면 <see langword="null"/>(→ 3003).</returns>
    public Error? Find(string? constraintName) =>
        string.IsNullOrEmpty(constraintName) ? null : _errors.GetValueOrDefault(constraintName);
}
