namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>
/// 변환되지 않은 DB 예외 시나리오입니다(S02-T07 "23514 · 25006은 분류하지 않음"). 메시지에 제약 이름 · 테이블 · SQL이 들어 있어,
/// 응답과 로그에 이 문자열이 하나도 나오지 않는지 단언하는 데 씁니다.
/// </summary>
internal static class UnconvertedDbFailures
{
    public const string CheckConstraintName = "ck_employee_accounts_status";

    public const string TableName = "employee_accounts";

    public const string Sql = "INSERT INTO employee_accounts (id, email) VALUES (@p0, @p1)";

    public const string SecretValue = "hong@example.com";

    public static readonly IReadOnlyList<string> SensitiveFragments = [CheckConstraintName, TableName, Sql, SecretValue, "23514", "25006", "read-only"];

    /// <summary>체크 제약 위반(23514)을 DbUpdateException처럼 감싼 예외를 던져 스택 트레이스가 있는 상태로 돌려줍니다.</summary>
    public static Exception CheckViolation() => Capture(() =>
        throw new InvalidOperationException(
            $"An error occurred while saving the entity changes. {Sql}",
            new SampleDbException(
                $"23514: new row for relation \"{TableName}\" violates check constraint \"{CheckConstraintName}\" DETAIL: Failing row contains ({SecretValue}).",
                "23514")));

    /// <summary>읽기 전용 트랜잭션 쓰기 거부(25006)를 던져 스택 트레이스가 있는 상태로 돌려줍니다.</summary>
    public static Exception ReadOnlyViolation() => Capture(() =>
        throw new SampleDbException($"25006: cannot execute INSERT in a read-only transaction ({Sql})", "25006"));

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
