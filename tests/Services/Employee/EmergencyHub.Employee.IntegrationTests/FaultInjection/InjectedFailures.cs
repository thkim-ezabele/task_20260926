using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>
/// 인터셉터가 던질 DB 예외입니다(testing-strategy.md "장애 주입": 서버에 없는 조건 · 트리거 대안). 둘 다 Npgsql 실행 전략이 일시 오류로 보고 재시도합니다.
/// </summary>
public static class InjectedFailures
{
    /// <summary>직렬화 실패(<c>40001</c>) <see cref="PostgresException"/>입니다(<c>IsTransient</c> = true).</summary>
    /// <returns>새 예외.</returns>
    public static PostgresException SerializationFailure() =>
        new("test fault: injected serialization failure", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure);

    /// <summary>시간 초과를 원인으로 가진 <see cref="NpgsqlException"/>입니다(<c>IsTransient</c> = true).</summary>
    /// <returns>새 예외.</returns>
    public static NpgsqlException TransientTimeout() => new("test fault: injected timeout", new TimeoutException());
}
