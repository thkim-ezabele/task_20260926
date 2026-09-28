namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>대상 대기 목록의 항목 하나: 제품 대상이 잠시 0개인 규칙과 대상을 되돌릴 작업 ID.</summary>
/// <param name="Rule">대기 중인 규칙(같은 인스턴스로 찾는다).</param>
/// <param name="ReleaseTaskId">대상이 생기고 목록에서 빼는 작업 ID(예: <c>S06-T04</c>).</param>
public sealed record PendingTargetRule(ArchitectureRule Rule, string ReleaseTaskId);
