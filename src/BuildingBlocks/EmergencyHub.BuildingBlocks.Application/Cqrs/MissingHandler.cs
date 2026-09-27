namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Handler 등록 누락 예외를 만듭니다. 누락은 예상할 수 없는 프로그래밍 오류라 <c>Result</c>가 아니라 예외입니다(ADR-0015).
/// </summary>
internal static class MissingHandler
{
    public static InvalidOperationException Create(Type requestType, Type handlerType) =>
        new($"요청 형식 '{requestType.FullName}'의 Handler가 등록되어 있지 않습니다. 필요한 서비스: {handlerType}");
}
