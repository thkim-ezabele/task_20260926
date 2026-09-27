namespace EmergencyHub.ArchitectureTests.Samples.ServiceIsolationOrdering;

/// <summary>표본용 "다른 서비스" 형식(서비스 격리 규칙의 금지 대상 네임스페이스).</summary>
/// <param name="Number">주문 번호.</param>
public sealed record OrderSnapshot(string Number);
