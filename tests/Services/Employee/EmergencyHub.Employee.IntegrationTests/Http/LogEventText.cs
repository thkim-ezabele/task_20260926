using System.Globalization;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>수집한 Serilog 이벤트를 노출 검사용 문자열로 바꾸고 이벤트 ID로 찾습니다(H4 · S2 로그 단언).</summary>
internal static class LogEventText
{
    /// <summary>
    /// 메시지 템플릿 · 렌더링한 메시지 · 모든 속성 값 · 예외 <see cref="Exception.ToString"/>을 한 문자열로 이어 붙입니다.
    /// 콘솔 · 파일 · OTLP 싱크가 내보낼 수 있는 내용을 모두 포함합니다.
    /// </summary>
    /// <param name="logEvent">이벤트.</param>
    /// <returns>검사용 문자열.</returns>
    public static string Dump(LogEvent logEvent) =>
        string.Join(
            '\n',
            [
                logEvent.MessageTemplate.Text,
                logEvent.RenderMessage(CultureInfo.InvariantCulture),
                .. logEvent.Properties.Select(property => $"{property.Key}={property.Value}"),
                logEvent.Exception?.ToString() ?? string.Empty,
            ]);

    /// <summary><c>SourceContext</c> 속성 값(따옴표 없음)입니다. 없으면 빈 문자열입니다.</summary>
    /// <param name="logEvent">이벤트.</param>
    /// <returns>범주 이름.</returns>
    public static string SourceContext(this LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("SourceContext", out var value) && value is ScalarValue { Value: string text } ? text : string.Empty;

    /// <summary>Microsoft.Extensions.Logging 이벤트 ID(<c>EventId.Id</c>)입니다. 없으면 <see langword="null"/>입니다.</summary>
    /// <param name="logEvent">이벤트.</param>
    /// <returns>이벤트 ID.</returns>
    public static int? EventId(this LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("EventId", out var value)
        && value is StructureValue structure
        && structure.Properties.FirstOrDefault(property => property.Name == "Id")?.Value is ScalarValue { Value: int id }
            ? id
            : null;

    /// <summary>
    /// 속성의 스칼라 값을 렌더링 전 원래 값(<see cref="object.ToString"/>, 따옴표 · 이스케이프 없음)으로 모두 펼칩니다(구조 · 목록 · 사전 안쪽 포함).
    /// </summary>
    /// <param name="logEvent">이벤트.</param>
    /// <returns>값 목록.</returns>
    public static IReadOnlyList<string> RawValues(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var values = new List<string>();
        foreach (var property in logEvent.Properties.Values)
        {
            CollectRaw(property, values);
        }

        return values;
    }

    /// <summary>
    /// Error 이상(Error · Fatal) 이벤트 중 이벤트 ID가 <paramref name="expectedEventIds"/>에 없는 것을 돌려줍니다(S06-T06 인계 메모: 테스트가 일부러 낸 로그만 제외).
    /// </summary>
    /// <param name="events">수집한 이벤트.</param>
    /// <param name="expectedEventIds">테스트가 일부러 낸 로그의 이벤트 ID(단언한 뒤 제외). 이벤트 ID가 없는 이벤트는 제외되지 않습니다.</param>
    /// <returns>판정받을 이벤트.</returns>
    public static IReadOnlyList<LogEvent> UnexpectedErrors(this IEnumerable<LogEvent> events, params int[] expectedEventIds)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(expectedEventIds);

        return [.. events.Where(logEvent => logEvent.Level >= LogEventLevel.Error && !(logEvent.EventId() is { } id && expectedEventIds.Contains(id)))];
    }

    private static void CollectRaw(LogEventPropertyValue value, List<string> values)
    {
        switch (value)
        {
            case ScalarValue scalar:
                values.Add(Convert.ToString(scalar.Value, CultureInfo.InvariantCulture) ?? string.Empty);
                break;
            case StructureValue structure:
                foreach (var property in structure.Properties)
                {
                    CollectRaw(property.Value, values);
                }

                break;
            case SequenceValue sequence:
                foreach (var element in sequence.Elements)
                {
                    CollectRaw(element, values);
                }

                break;
            case DictionaryValue dictionary:
                foreach (var entry in dictionary.Elements)
                {
                    CollectRaw(entry.Key, values);
                    CollectRaw(entry.Value, values);
                }

                break;
            default:
                values.Add(value.ToString());
                break;
        }
    }
}
