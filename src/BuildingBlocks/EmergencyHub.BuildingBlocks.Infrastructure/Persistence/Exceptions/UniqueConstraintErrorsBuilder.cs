using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;

/// <summary>
/// 서비스가 자기 유니크 인덱스(<c>ux_</c>)의 23505 위반을 서비스 에러 코드로 매핑하는 빌더입니다(database.md "23505 매핑 레지스트리 계약").
/// </summary>
/// <remarks>
/// 보통 <c>AddUnitOfWork&lt;TContext&gt;(errors =&gt; errors.Map(EmployeeIndexNames.Email, EmployeeErrors.DuplicateEmail))</c>로 씁니다.
/// 키는 매핑의 <c>HasDatabaseName</c>과 같은 이름 상수를 참조합니다(문자열 리터럴 금지). 모든 <c>ux_</c>를 등록할 필요는 없습니다(없으면 3003).
/// </remarks>
public sealed class UniqueConstraintErrorsBuilder
{
    private readonly Dictionary<UniqueIndexName, Error> _mappings = [];

    /// <summary>
    /// 유니크 인덱스 하나를 오류에 매핑합니다.
    /// </summary>
    /// <param name="indexName">유니크 인덱스 이름(서비스 Infrastructure의 이름 상수).</param>
    /// <param name="error">위반 시 돌려줄 오류. <see cref="ErrorType.Conflict"/>(409)여야 합니다.</param>
    /// <returns>같은 빌더(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="indexName"/> 또는 <paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException"><paramref name="error"/>의 유형이 <see cref="ErrorType.Conflict"/>가 아닌 경우.</exception>
    /// <exception cref="InvalidOperationException">같은 인덱스를 이미 매핑한 경우(같은 오류여도).</exception>
    public UniqueConstraintErrorsBuilder Map(UniqueIndexName indexName, Error error)
    {
        ArgumentNullException.ThrowIfNull(indexName);
        ArgumentNullException.ThrowIfNull(error);

        if (error.Type != ErrorType.Conflict)
        {
            throw new ArgumentException(
                $"유니크 제약 위반은 충돌(409)이라 ErrorType.Conflict 오류만 매핑합니다: {indexName} → {error.Code}(ErrorType {(short)error.Type})",
                nameof(error));
        }

        if (!_mappings.TryAdd(indexName, error))
        {
            throw new InvalidOperationException($"유니크 인덱스 {indexName}를 두 번 매핑했습니다. 인덱스 하나에 오류 하나만 둡니다.");
        }

        return this;
    }

    /// <summary>
    /// 지금까지의 매핑으로 불변 레지스트리를 만듭니다. 이후 이 빌더를 바꿔도 만든 레지스트리는 바뀌지 않습니다.
    /// </summary>
    /// <returns>레지스트리.</returns>
    public UniqueConstraintErrorRegistry Build() => new(_mappings);
}
