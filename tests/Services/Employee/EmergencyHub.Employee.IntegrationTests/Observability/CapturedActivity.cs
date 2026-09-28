using System.Diagnostics;

namespace EmergencyHub.Employee.IntegrationTests.Observability;

/// <summary>
/// 끝난 span 하나의 사본입니다(<see cref="ActivityCollector"/>). 원본 <see cref="Activity"/>는 끝난 뒤 재사용될 수 있어 값만 복사합니다.
/// </summary>
/// <param name="SourceName">ActivitySource 이름(<c>Npgsql</c> · <c>Microsoft.AspNetCore</c>).</param>
/// <param name="OperationName">작업 이름.</param>
/// <param name="DisplayName">표시 이름(Npgsql은 DB 이름 또는 명령 요약, ASP.NET Core는 메서드 + 라우트).</param>
/// <param name="Tags">태그(키, 문자열로 바꾼 값). 순서는 span의 태그 순서입니다.</param>
/// <param name="Events">span 이벤트(이름과 태그 문자열, 예: <c>exception</c>의 <c>exception.message</c>).</param>
/// <param name="Status">상태 코드.</param>
/// <param name="StatusDescription">상태 설명(오류면 메시지일 수 있음).</param>
/// <param name="TraceId">trace ID(32자리 소문자 16진수, ProblemDetails <c>traceId</c>와 같은 형식).</param>
public sealed record CapturedActivity(
    string SourceName,
    string OperationName,
    string DisplayName,
    IReadOnlyList<KeyValuePair<string, string>> Tags,
    IReadOnlyList<string> Events,
    ActivityStatusCode Status,
    string? StatusDescription,
    string TraceId)
{
    /// <summary>태그 값을 찾습니다. 없으면 <see langword="null"/>입니다.</summary>
    /// <param name="key">태그 키(예: <c>db.statement</c>).</param>
    /// <returns>태그 값.</returns>
    public string? Tag(string key) => Tags.FirstOrDefault(tag => tag.Key == key).Value;

    /// <summary>노출 검사용으로 이름 · 태그 키와 값 · 이벤트 · 상태 설명을 모두 이은 텍스트입니다.</summary>
    /// <returns>검사용 텍스트.</returns>
    public string Dump() =>
        string.Join(
            '\n',
            [
                OperationName,
                DisplayName,
                .. Tags.Select(tag => $"{tag.Key}={tag.Value}"),
                .. Events,
                StatusDescription ?? string.Empty,
            ]);
}
