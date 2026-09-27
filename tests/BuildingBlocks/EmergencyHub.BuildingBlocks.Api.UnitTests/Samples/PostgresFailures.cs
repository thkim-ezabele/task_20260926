using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>
/// UnitOfWork가 변환하지 않고 다시 던지는 실제 영속성 예외입니다(S02-T07 규칙표: 23514 · 25006 → 재전파, 재시도 한도 초과 → 분류기 9003).
/// <see cref="SampleDbException"/> 대역과 달리 실제 Npgsql · EF Core 형식이라 실제 분류기와 로그 속성까지 확인할 수 있습니다.
/// 제약 이름 · 테이블 · SQL · 입력 값은 <see cref="UnconvertedDbFailures"/>의 문자열을 씁니다.
/// </summary>
internal static class PostgresFailures
{
    /// <summary>체크 제약 위반(23514)을 <see cref="DbUpdateException"/>으로 감싸 던진 뒤 돌려줍니다.</summary>
    public static Exception CheckViolation() => Capture(() =>
        throw new DbUpdateException(
            $"An error occurred while saving the entity changes. {UnconvertedDbFailures.Sql}",
            Postgres("23514", $"new row for relation \"{UnconvertedDbFailures.TableName}\" violates check constraint \"{UnconvertedDbFailures.CheckConstraintName}\"")));

    /// <summary>읽기 전용 트랜잭션 쓰기 거부(25006)를 <see cref="DbUpdateException"/>으로 감싸 던진 뒤 돌려줍니다.</summary>
    public static Exception ReadOnlyViolation() => Capture(() =>
        throw new DbUpdateException(
            "An error occurred while saving the entity changes.",
            Postgres("25006", $"cannot execute INSERT in a read-only transaction ({UnconvertedDbFailures.Sql})")));

    /// <summary>실행 전략의 재시도 한도 초과를 던진 뒤 돌려줍니다. 안쪽 예외에 SQL · 테이블 이름이 들어 있습니다.</summary>
    public static Exception RetryLimitExceeded() => Capture(() =>
        throw new RetryLimitExceededException(
            "Maximum number of retries (6) exceeded while executing database operations with 'NpgsqlRetryingExecutionStrategy'.",
            new NpgsqlException($"Exception while reading from stream ({UnconvertedDbFailures.Sql})")));

    private static PostgresException Postgres(string sqlState, string messageText) =>
        new(
            messageText,
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState,
            detail: $"Failing row contains ({UnconvertedDbFailures.SecretValue}).",
            internalQuery: UnconvertedDbFailures.Sql,
            schemaName: "public",
            tableName: UnconvertedDbFailures.TableName,
            constraintName: UnconvertedDbFailures.CheckConstraintName);

    private static Exception Capture(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("예외가 발생하지 않았습니다.");
    }
}
