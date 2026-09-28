namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary><c>test_</c>로 시작하는 테스트 전용 DB 객체 수입니다(잔여 검사, 정리 뒤 모두 0).</summary>
/// <param name="Triggers">트리거 수(내부 트리거 제외).</param>
/// <param name="Functions"><c>public</c> 함수 수.</param>
/// <param name="Relations"><c>public</c> 테이블 · 시퀀스 등 릴레이션 수.</param>
public sealed record TestObjectCounts(long Triggers, long Functions, long Relations)
{
    /// <summary>남은 객체가 없는 상태입니다.</summary>
    public static TestObjectCounts None { get; } = new(0, 0, 0);
}
