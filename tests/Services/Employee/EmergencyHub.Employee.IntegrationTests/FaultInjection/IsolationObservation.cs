namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>P1 관찰 트리거가 <c>INSERT</c> 때 기록한 서버 트랜잭션 설정입니다(<c>current_setting</c> 값 그대로).</summary>
/// <param name="TransactionIsolation"><c>transaction_isolation</c>(예: <c>read committed</c>).</param>
/// <param name="TransactionReadOnly"><c>transaction_read_only</c>(<c>on</c> / <c>off</c>).</param>
/// <param name="DefaultTransactionIsolation"><c>default_transaction_isolation</c>(연결 옵션으로 바꾼 서버 기본값).</param>
public sealed record IsolationObservation(string TransactionIsolation, string TransactionReadOnly, string DefaultTransactionIsolation);
