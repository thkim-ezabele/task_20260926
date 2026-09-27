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
}
