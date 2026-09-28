using System.Text.Json;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>
/// 응답 본문 · 로그 · 문자열에서 입력한 개인정보 값(이름 · 이메일 · 전화번호)을 찾습니다(PRD-002 NFR-04 · FR-10 개인정보 테스트, S06-T06).
/// </summary>
/// <remarks>
/// 비교는 대소문자 무시 서수 부분 문자열입니다(이메일 원문 · 정규화 값을 모두 잡음). JSON은 원문 텍스트와 <b>디코딩한</b> 모든 문자열 값 · 속성 이름을 함께 봅니다
/// (System.Text.Json 기본 인코더가 한글을 <c>\uXXXX</c>로 이스케이프하므로 원문 검색만으로는 이름을 놓침).
/// 로그는 <see cref="LogEventText.Dump"/>(템플릿 · 렌더링 메시지 · 속성 · 예외)와 속성의 원래 스칼라 값(렌더링 전)을 봅니다.
/// 결과가 비어 있으면 노출 없음입니다. 빈 문자열 값은 검색하지 않습니다.
/// </remarks>
public static class PersonalDataScan
{
    /// <summary>텍스트에 들어 있는 값을 돌려줍니다(입력 순서, 중복 제거).</summary>
    /// <param name="text">검사할 텍스트.</param>
    /// <param name="values">찾을 값.</param>
    /// <returns>찾은 값.</returns>
    public static IReadOnlyList<string> FindIn(string text, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(text);

        return Find([text], values);
    }

    /// <summary>JSON 본문(원문 + 디코딩한 문자열 · 속성 이름)에 들어 있는 값을 돌려줍니다.</summary>
    /// <param name="json">JSON 텍스트.</param>
    /// <param name="values">찾을 값.</param>
    /// <returns>찾은 값.</returns>
    /// <exception cref="JsonException"><paramref name="json"/>이 올바른 JSON이 아닌 경우.</exception>
    public static IReadOnlyList<string> FindInJson(string json, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);
        var texts = new List<string> { json };
        CollectStrings(document.RootElement, texts);
        return Find(texts, values);
    }

    /// <summary>로그 이벤트마다 들어 있는 값을 돌려줍니다(이벤트 순서, 이벤트 안에서는 입력 순서).</summary>
    /// <param name="events">수집한 이벤트(<see cref="Fixtures.SerilogEventCollector.Events"/>).</param>
    /// <param name="values">찾을 값.</param>
    /// <returns>노출 목록.</returns>
    public static IReadOnlyList<LogExposure> FindInLogs(IEnumerable<LogEvent> events, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(values);

        var searched = values.ToList();
        var exposures = new List<LogExposure>();
        foreach (var logEvent in events)
        {
            var texts = new List<string> { LogEventText.Dump(logEvent) };
            texts.AddRange(LogEventText.RawValues(logEvent));
            exposures.AddRange(Find(texts, searched).Select(value =>
                new LogExposure(logEvent.EventId(), logEvent.Level, logEvent.SourceContext(), logEvent.MessageTemplate.Text, value)));
        }

        return exposures;
    }

    private static List<string> Find(IReadOnlyCollection<string> texts, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return
        [
            .. values
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Where(value => texts.Any(text => text.Contains(value, StringComparison.OrdinalIgnoreCase))),
        ];
    }

    private static void CollectStrings(JsonElement element, List<string> texts)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    texts.Add(property.Name);
                    CollectStrings(property.Value, texts);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectStrings(item, texts);
                }

                break;
            case JsonValueKind.String:
                texts.Add(element.GetString() ?? string.Empty);
                break;
            default:
                break;
        }
    }
}
