using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// <see cref="EmployeeApiFactory"/> 호스트의 Serilog 이벤트를 모으는 싱크입니다. DI에 <see cref="ILogEventSink"/>로 등록하면
/// ServiceDefaults의 <c>ReadFrom.Services</c>가 받습니다(ADR-0020, S03-T03). 최소 수준 · 범주 재정의는 Api 설정 그대로 적용된 뒤에 들어옵니다.
/// </summary>
public sealed class SerilogEventCollector : ILogEventSink
{
    /// <summary>Serilog 요청 완료 로그(<c>UseSerilogRequestLogging</c>)의 메시지 템플릿입니다(Serilog.AspNetCore 기본값).</summary>
    public const string RequestCompletionTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    private readonly ConcurrentQueue<LogEvent> _events = new();

    /// <summary>지금까지 받은 이벤트의 사본입니다(받은 순서).</summary>
    public IReadOnlyList<LogEvent> Events => [.. _events];

    /// <summary>요청 완료 로그 이벤트만 돌려줍니다(<see cref="RequestCompletionTemplate"/>).</summary>
    public IReadOnlyList<LogEvent> RequestCompletions =>
        [.. _events.Where(logEvent => logEvent.MessageTemplate.Text == RequestCompletionTemplate)];

    /// <summary>
    /// 경로가 <paramref name="path"/>인 요청 완료 로그의 HTTP 상태 코드를 받은 순서로 돌려줍니다(서버 쪽 최종 상태, S06-T06).
    /// </summary>
    /// <remarks>
    /// Kestrel이 본문 한도 초과로 응답을 쓴 뒤 연결을 닫으면 클라이언트는 <see cref="HttpRequestException"/>만 받을 수 있습니다. 그때도 서버가 쓴 상태는 이 목록에 남습니다.
    /// </remarks>
    /// <param name="path">요청 경로(쿼리 문자열 없음, 대소문자 구분).</param>
    /// <returns>상태 코드 목록.</returns>
    public IReadOnlyList<int> RequestStatusCodes(string path) =>
        [.. RequestCompletions
            .Where(logEvent => logEvent.Properties.TryGetValue("RequestPath", out var value) && value is ScalarValue { Value: string requestPath } && requestPath == path)
            .Select(logEvent => logEvent.Properties["StatusCode"] is ScalarValue { Value: int statusCode }
                ? statusCode
                : throw new InvalidOperationException("요청 완료 로그의 StatusCode가 정수가 아닙니다."))];

    /// <inheritdoc/>
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        _events.Enqueue(logEvent);
    }

    /// <summary>모은 이벤트를 비웁니다(호스트 시작 로그를 빼고 요청 한 건만 보고 싶을 때).</summary>
    public void Clear() => _events.Clear();
}
