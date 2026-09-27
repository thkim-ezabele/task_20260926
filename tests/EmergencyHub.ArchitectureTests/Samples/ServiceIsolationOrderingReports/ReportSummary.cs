namespace EmergencyHub.ArchitectureTests.Samples.ServiceIsolationOrderingReports;

/// <summary>
/// 표본용 형식: 이름이 금지 서비스 네임스페이스(<c>...ServiceIsolationOrdering</c>)로 시작하지만 점 경계가 달라 다른 서비스다.
/// 서비스 격리 규칙이 접두사를 점 단위로 비교하는지 확인한다.
/// </summary>
/// <param name="Count">건수.</param>
public sealed record ReportSummary(int Count);
