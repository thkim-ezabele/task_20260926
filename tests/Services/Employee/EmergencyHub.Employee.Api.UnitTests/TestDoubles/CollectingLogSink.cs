using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace EmergencyHub.Employee.Api.UnitTests.TestDoubles;

/// <summary>DI에 <see cref="ILogEventSink"/>로 등록하면 ServiceDefaults의 <c>ReadFrom.Services</c>가 붙이는 수집 싱크입니다.</summary>
internal sealed class CollectingLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyList<LogEvent> Events => [.. _events];

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
}
