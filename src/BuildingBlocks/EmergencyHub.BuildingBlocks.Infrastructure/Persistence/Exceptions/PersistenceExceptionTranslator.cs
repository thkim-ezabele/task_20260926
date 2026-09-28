using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;

/// <summary>
/// UnitOfWork가 쓰는 영속성 예외 → <see cref="Error"/> 변환 순수 함수입니다(database.md "영속성 예외 변환" 규칙표 1 ~ 9, ADR-0014).
/// </summary>
/// <remarks>
/// <para>
/// 판정은 형식 검사와 Npgsql 상수(<see cref="PostgresErrorCodes.UniqueViolation"/>)로 하고 형식 이름 · SqlState 문자열 리터럴을 비교하지 않습니다.
/// 위에서부터 처음 맞는 규칙을 씁니다: <see cref="DbUpdateConcurrencyException"/>(<see cref="DbUpdateException"/> 파생)을 먼저 검사합니다.
/// </para>
/// <para>
/// <see cref="Exception.InnerException"/>은 바로 한 단계만 봅니다. 23514 · 그 밖의 SqlState · 안쪽이 <see cref="PostgresException"/>이 아닌 경우 ·
/// 감싸지 않은 <see cref="PostgresException"/>(COMMIT) · 재시도 한도 초과 · 취소는 변환하지 않습니다(<see langword="null"/>).
/// </para>
/// </remarks>
internal static class PersistenceExceptionTranslator
{
    /// <summary>
    /// 예외를 변환합니다.
    /// </summary>
    /// <param name="exception">UnitOfWork가 실행 전략 바깥에서 받은 예외.</param>
    /// <param name="uniqueConstraintErrors">서비스 23505 매핑 레지스트리.</param>
    /// <returns>변환 결과. 변환 대상이 아니면 <see langword="null"/>(호출자가 예외를 그대로 올림).</returns>
    /// <exception cref="ArgumentNullException">인자가 <see langword="null"/>인 경우.</exception>
    public static PersistenceExceptionTranslation? Translate(Exception exception, UniqueConstraintErrorRegistry uniqueConstraintErrors)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(uniqueConstraintErrors);

        return exception switch
        {
            DbUpdateConcurrencyException concurrency => new(
                CommonErrors.ConcurrencyConflict,
                PersistenceFailureKind.ConcurrencyConflict,
                ConstraintName: null,
                SqlState: null,
                EntityTypeNames(concurrency)),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } unique } update =>
                TranslateUniqueViolation(update, unique, uniqueConstraintErrors),
            _ => null,
        };
    }

    private static PersistenceExceptionTranslation TranslateUniqueViolation(
        DbUpdateException update,
        PostgresException unique,
        UniqueConstraintErrorRegistry uniqueConstraintErrors) =>
        uniqueConstraintErrors.Find(unique.ConstraintName) is { } mapped
            ? new(mapped, PersistenceFailureKind.MappedUniqueViolation, unique.ConstraintName, unique.SqlState, EntityTypeNames(update))
            : new(CommonErrors.UniqueConstraintViolated, PersistenceFailureKind.UnmappedUniqueViolation, unique.ConstraintName, unique.SqlState, EntityTypeNames(update));

    private static List<string> EntityTypeNames(DbUpdateException exception) =>
        [.. exception.Entries.Select(entry => entry.Metadata.ClrType.Name).Distinct(StringComparer.Ordinal)];
}
