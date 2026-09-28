using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace EmergencyHub.Employee.IntegrationTests.Observability;

/// <summary>
/// Npgsql 추적 span(ActivitySource <c>Npgsql</c>)을 <see cref="ActivityListener"/>로 모읍니다(PRD-002 NFR-04 BL-024: <c>db.connection_string</c> · <c>db.statement</c> 등
/// 태그에 DB 비밀번호 · 입력 값이 없음을 실측, S06-T06).
/// </summary>
/// <remarks>
/// <para>만들 때 리스너를 등록하고 <see cref="Dispose"/> 때 해제합니다. 샘플링은 <see cref="ActivitySamplingResult.AllDataAndRecorded"/>라 OTLP exporter가 없어도 태그가 채워집니다.</para>
/// <para>리스너는 프로세스 전체에 걸립니다. 같은 컬렉션 테스트는 순차 실행이므로 테스트 안에서 만들고 끝나면 폐기하면 그 테스트의 span만 남습니다.
/// 필요하면 <paramref name="filter"/>(예: <c>db.name</c>)로 좁힙니다.</para>
/// </remarks>
public sealed class NpgsqlActivityCollector : IDisposable
{
    /// <summary>Npgsql ActivitySource 이름입니다(Npgsql 8, <c>NpgsqlActivitySource</c>).</summary>
    public const string SourceName = "Npgsql";

    private readonly ConcurrentQueue<CapturedActivity> _activities = new();
    private readonly ActivityListener _listener;
    private readonly Func<Activity, bool>? _filter;

    /// <summary>리스너를 등록합니다.</summary>
    /// <param name="filter">모을 span 조건(끝난 span 기준). <see langword="null"/>이면 Npgsql span을 모두 모읍니다.</param>
    public NpgsqlActivityCollector(Func<Activity, bool>? filter = null)
    {
        _filter = filter;
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = OnStopped,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>지금까지 끝난 span의 사본입니다(끝난 순서).</summary>
    public IReadOnlyList<CapturedActivity> Activities => [.. _activities];

    /// <summary>모은 span을 비웁니다(호스트 시작 · 헬스 검사 span을 빼고 요청 하나만 보고 싶을 때).</summary>
    public void Clear() => _activities.Clear();

    /// <summary>리스너를 해제합니다. 해제 뒤에는 더 모으지 않습니다.</summary>
    public void Dispose() => _listener.Dispose();

    private static CapturedActivity Snapshot(Activity activity) =>
        new(
            activity.Source.Name,
            activity.OperationName,
            activity.DisplayName,
            [.. activity.TagObjects.Select(tag => new KeyValuePair<string, string>(tag.Key, Text(tag.Value)))],
            [.. activity.Events.Select(activityEvent =>
                string.Join('\n', [activityEvent.Name, .. activityEvent.Tags.Select(tag => $"{tag.Key}={Text(tag.Value)}")]))],
            activity.Status,
            activity.StatusDescription);

    private static string Text(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private void OnStopped(Activity activity)
    {
        if (_filter is null || _filter(activity))
        {
            _activities.Enqueue(Snapshot(activity));
        }
    }
}
