using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace EmergencyHub.Employee.IntegrationTests.Observability;

/// <summary>
/// ActivitySource 하나의 추적 span을 <see cref="ActivityListener"/>로 모읍니다(PRD-002 NFR-04).
/// Npgsql span(<see cref="NpgsqlSource"/>, BL-024: <c>db.connection_string</c> · <c>db.statement</c>에 비밀번호 · 입력 값 없음, S06-T06)과
/// ASP.NET Core 요청 span(<see cref="AspNetCoreSource"/>, <c>url.path</c>가 라우트 템플릿인지, S07-T03)에 씁니다.
/// </summary>
/// <remarks>
/// <para>만들 때 리스너를 등록하고 <see cref="Dispose"/> 때 해제합니다. 샘플링은 <see cref="ActivitySamplingResult.AllDataAndRecorded"/>라 OTLP exporter가 없어도 태그가 채워집니다.
/// ASP.NET Core span의 <c>url.path</c> 덮어쓰기(ServiceDefaults <c>EnrichWithHttpResponse</c>)는 호스트의 OpenTelemetry 계측이 하며 exporter가 없어도 동작합니다.</para>
/// <para>리스너는 프로세스 전체에 걸립니다. 같은 컬렉션 테스트는 순차 실행이므로 테스트 안에서 만들고 끝나면 폐기하면 그 테스트의 span만 남습니다.
/// 요청 span은 <see cref="ServerPort"/>(Kestrel 호스트: 같은 프로세스 HttpClient 계측이 <c>traceparent</c>를 덮어 trace ID로 고를 수 없음)
/// 또는 <see cref="CapturedActivity.TraceId"/>(TestServer 클라이언트: HttpClient 계측이 없어 보낸 <c>traceparent</c>의 trace ID를 그대로 이음)로 고릅니다.</para>
/// <para>요청 span은 응답을 보낸 뒤 끝나므로 응답을 받은 직후에는 아직 없을 수 있습니다. <see cref="WaitForAsync"/>로 기다립니다.</para>
/// </remarks>
public sealed class ActivityCollector : IDisposable
{
    /// <summary>Npgsql ActivitySource 이름입니다(Npgsql 8, <c>NpgsqlActivitySource</c>).</summary>
    public const string NpgsqlSource = "Npgsql";

    /// <summary>ASP.NET Core 요청 span의 ActivitySource 이름입니다(호스팅 진단, OpenTelemetry ASP.NET Core 계측이 태그를 채움).</summary>
    public const string AspNetCoreSource = "Microsoft.AspNetCore";

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(5);

    private readonly ConcurrentQueue<CapturedActivity> _activities = new();
    private readonly ActivityListener _listener;
    private readonly Func<Activity, bool>? _filter;

    /// <summary>리스너를 등록합니다.</summary>
    /// <param name="sourceName">모을 ActivitySource 이름(<see cref="NpgsqlSource"/> · <see cref="AspNetCoreSource"/>).</param>
    /// <param name="filter">모을 span 조건(끝난 span 기준). <see langword="null"/>이면 그 소스의 span을 모두 모읍니다.</param>
    /// <exception cref="ArgumentException"><paramref name="sourceName"/>이 비었거나 공백뿐인 경우.</exception>
    public ActivityCollector(string sourceName, Func<Activity, bool>? filter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        _filter = filter;
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = OnStopped,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>지금까지 끝난 span의 사본입니다(끝난 순서).</summary>
    public IReadOnlyList<CapturedActivity> Activities => [.. _activities];

    /// <summary>
    /// 서버 포트 태그(<c>server.port</c>)가 <paramref name="port"/>인 span인지 보는 조건입니다(Kestrel 호스트 요청 span 고르기, <c>factory.KestrelAddress.Port</c>).
    /// </summary>
    /// <param name="port">포트.</param>
    /// <returns>조건.</returns>
    public static Func<CapturedActivity, bool> ServerPort(int port)
    {
        var text = port.ToString(CultureInfo.InvariantCulture);
        return activity => activity.Tag("server.port") == text;
    }

    /// <summary>
    /// <paramref name="predicate"/>에 맞는 span이 <paramref name="count"/>개 이상 모일 때까지 기다린 뒤 맞는 span을 모두 돌려줍니다.
    /// 시간 안에 모이지 않으면 그때까지 맞은 span만 돌려줍니다(개수는 호출자가 단언).
    /// </summary>
    /// <param name="predicate">고를 조건(예: <see cref="ServerPort"/>, <c>span =&gt; span.TraceId == traceId</c>).</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <param name="count">기다릴 개수(1 이상).</param>
    /// <param name="timeout">최대 대기 시간. <see langword="null"/>이면 5초입니다.</param>
    /// <returns>맞는 span(끝난 순서).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/>가 1보다 작은 경우.</exception>
    public async Task<IReadOnlyList<CapturedActivity>> WaitForAsync(
        Func<CapturedActivity, bool> predicate,
        CancellationToken cancellationToken,
        int count = 1,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var started = Stopwatch.GetTimestamp();
        var limit = timeout ?? DefaultWaitTimeout;
        while (true)
        {
            IReadOnlyList<CapturedActivity> matched = [.. _activities.Where(predicate)];
            if (matched.Count >= count || Stopwatch.GetElapsedTime(started) >= limit)
            {
                return matched;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

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
            activity.StatusDescription,
            activity.TraceId.ToHexString());

    private static string Text(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private void OnStopped(Activity activity)
    {
        if (_filter is null || _filter(activity))
        {
            _activities.Enqueue(Snapshot(activity));
        }
    }
}
