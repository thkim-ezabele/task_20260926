namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>
/// 테스트가 일부러 낸 로그를 가리키는 이벤트 ID + 범주(<c>SourceContext</c>)입니다(<see cref="LogEventText.UnexpectedErrors(IEnumerable{Serilog.Events.LogEvent}, ExpectedLogEvent, ExpectedLogEvent[])"/>, BL-139).
/// </summary>
/// <remarks>이벤트 ID는 범주마다 따로 매겨져(예: 프레임워크 <c>RequestSizeLimitFilter</c>도 ID 1) ID만으로는 다른 범주의 로그까지 제외할 수 있습니다.</remarks>
/// <param name="EventId">이벤트 ID(<c>EventId.Id</c>).</param>
/// <param name="SourceContext">로거 범주(서수 비교).</param>
public sealed record ExpectedLogEvent(int EventId, string SourceContext)
{
    /// <summary>전역 예외 처리기 로그(이벤트 ID 1, <c>BuildingBlocks.Api</c> <c>GlobalExceptionHandler</c>, 500 · 9001)입니다.</summary>
    public static ExpectedLogEvent GlobalException { get; } = new(1, "EmergencyHub.BuildingBlocks.Api.Exceptions.GlobalExceptionHandler");

    /// <summary>이벤트가 이 ID와 범주를 모두 갖는지 봅니다.</summary>
    /// <param name="logEvent">이벤트.</param>
    /// <returns>둘 다 같으면 <see langword="true"/>.</returns>
    public bool Matches(Serilog.Events.LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        return logEvent.EventId() == EventId && string.Equals(logEvent.SourceContext(), SourceContext, StringComparison.Ordinal);
    }
}
