namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>
/// 테스트 전용 트리거 SQL 원문입니다(testing-strategy.md "장애 주입" SQL 그대로, 이름 <c>test_fault_*</c> · <c>test_probe_*</c>).
/// 운영 스키마 · 마이그레이션에는 넣지 않습니다. 실행은 <see cref="TestTriggers"/>가 <c>employee_app</c> 쓰기 연결로 합니다.
/// </summary>
public static class TestTriggerSql
{
    /// <summary>P2: 커밋 시점 40001 1회(시퀀스로 시도 수를 셈, <c>DEFERRABLE INITIALLY DEFERRED</c>는 fixture 안 예외).</summary>
    public const string CreateCommitFailureOnce = """
        CREATE SEQUENCE test_fault_p2_attempts;
        CREATE FUNCTION test_fault_p2_fail_first_commit() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF nextval('test_fault_p2_attempts') = 1 THEN
                RAISE EXCEPTION 'test fault P2: commit-time serialization failure' USING ERRCODE = '40001';
            END IF;
            RETURN NULL;
        END
        $$;
        CREATE CONSTRAINT TRIGGER test_fault_p2_fail_first_commit
            AFTER INSERT ON employees DEFERRABLE INITIALLY DEFERRED
            FOR EACH ROW EXECUTE FUNCTION test_fault_p2_fail_first_commit();
        """;

    /// <summary>P2 시도 수 시퀀스 이름입니다.</summary>
    public const string CommitFailureOnceAttempts = "test_fault_p2_attempts";

    /// <summary>P6: 매번 40001(<c>BEFORE INSERT</c>).</summary>
    public const string CreateAlwaysFail = """
        CREATE SEQUENCE test_fault_p6_attempts;
        CREATE FUNCTION test_fault_p6_always_fail() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            PERFORM nextval('test_fault_p6_attempts');
            RAISE EXCEPTION 'test fault P6: persistent serialization failure' USING ERRCODE = '40001';
        END
        $$;
        CREATE TRIGGER test_fault_p6_always_fail
            BEFORE INSERT ON employees FOR EACH ROW EXECUTE FUNCTION test_fault_p6_always_fail();
        """;

    /// <summary>P6 시도 수 시퀀스 이름입니다.</summary>
    public const string AlwaysFailAttempts = "test_fault_p6_attempts";

    /// <summary>P1: 트랜잭션 설정 관찰(Write 연결 <c>Options=-c default_transaction_isolation=serializable</c>로 커밋).</summary>
    public const string CreateIsolationProbe = """
        CREATE TABLE test_probe_p1_observations (
            transaction_isolation text NOT NULL,
            transaction_read_only text NOT NULL,
            default_transaction_isolation text NOT NULL
        );
        CREATE FUNCTION test_probe_p1_isolation() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            INSERT INTO test_probe_p1_observations
            VALUES (current_setting('transaction_isolation'), current_setting('transaction_read_only'),
                    current_setting('default_transaction_isolation'));
            RETURN NEW;
        END
        $$;
        CREATE TRIGGER test_probe_p1_isolation
            BEFORE INSERT ON employees FOR EACH ROW EXECUTE FUNCTION test_probe_p1_isolation();
        """;

    /// <summary>P1 관찰 조회입니다.</summary>
    public const string SelectIsolationObservations =
        "SELECT transaction_isolation, transaction_read_only, default_transaction_isolation FROM test_probe_p1_observations";

    /// <summary>finally 정리(트리거 → 함수 → 시퀀스 · 테이블, 모두 <c>IF EXISTS</c>라 여러 번 실행해도 됨).</summary>
    public const string DropAll = """
        DROP TRIGGER IF EXISTS test_fault_p2_fail_first_commit ON employees;
        DROP FUNCTION IF EXISTS test_fault_p2_fail_first_commit();
        DROP SEQUENCE IF EXISTS test_fault_p2_attempts;
        DROP TRIGGER IF EXISTS test_fault_p6_always_fail ON employees;
        DROP FUNCTION IF EXISTS test_fault_p6_always_fail();
        DROP SEQUENCE IF EXISTS test_fault_p6_attempts;
        DROP TRIGGER IF EXISTS test_probe_p1_isolation ON employees;
        DROP FUNCTION IF EXISTS test_probe_p1_isolation();
        DROP TABLE IF EXISTS test_probe_p1_observations;
        """;

    /// <summary>잔여 검사(트리거 · 함수 · 릴레이션 수, 모두 0이어야 함).</summary>
    public const string CountLeftovers = """
        SELECT (SELECT count(*) FROM pg_trigger WHERE tgname LIKE 'test\_%' AND NOT tgisinternal),
               (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
                 WHERE n.nspname = 'public' AND p.proname LIKE 'test\_%'),
               (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                 WHERE n.nspname = 'public' AND c.relname LIKE 'test\_%');
        """;
}
