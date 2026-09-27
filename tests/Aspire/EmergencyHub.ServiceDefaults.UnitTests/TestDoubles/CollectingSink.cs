using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;

// DI에 ILogEventSink로 등록하면 SerilogDefaults의 ReadFrom.Services가 싱크로 붙인다(S03-T07 통합 테스트와 같은 방식).
internal sealed class CollectingSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyList<LogEvent> Events => [.. _events];

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
}
