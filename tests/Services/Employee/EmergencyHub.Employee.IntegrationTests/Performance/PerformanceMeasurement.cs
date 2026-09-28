using System.Diagnostics;

namespace EmergencyHub.Employee.IntegrationTests.Performance;

/// <summary>
/// 워밍업 뒤 N회 실행해 시간을 재는 도우미입니다(PRD-002 NFR-02 · 03, S06-T06: 워밍업 1회 뒤 N회 중앙값).
/// </summary>
/// <remarks>
/// 회차마다 <c>prepare</c>(측정 제외, 예: DB 초기화)를 먼저 부르고 <c>action</c>만 <see cref="Stopwatch"/>로 잽니다. 워밍업도 같은 순서로 실행합니다.
/// 측정 중 <c>EnableSensitiveDataLogging</c>이 꺼져 있는지는 <see cref="DbContextOptionsInspection.IsSensitiveDataLoggingEnabled"/>로 따로 확인합니다.
/// </remarks>
public static class PerformanceMeasurement
{
    /// <summary>측정합니다.</summary>
    /// <param name="warmupCount">워밍업 횟수(0 이상).</param>
    /// <param name="iterationCount">측정 횟수(1 이상).</param>
    /// <param name="prepare">회차마다 측정 전에 부를 준비 작업.</param>
    /// <param name="action">잴 작업.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>워밍업 · 측정 시간.</returns>
    /// <exception cref="ArgumentOutOfRangeException">횟수가 범위 밖인 경우.</exception>
    public static async Task<MeasurementResult> MeasureAsync(
        int warmupCount,
        int iterationCount,
        Func<CancellationToken, Task> prepare,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(warmupCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterationCount, 1);
        ArgumentNullException.ThrowIfNull(prepare);
        ArgumentNullException.ThrowIfNull(action);

        var warmups = new List<TimeSpan>(warmupCount);
        for (var run = 0; run < warmupCount; run++)
        {
            warmups.Add(await RunOnceAsync(prepare, action, cancellationToken));
        }

        var samples = new List<TimeSpan>(iterationCount);
        for (var run = 0; run < iterationCount; run++)
        {
            samples.Add(await RunOnceAsync(prepare, action, cancellationToken));
        }

        return new MeasurementResult(warmups, samples);
    }

    /// <summary>
    /// 준비 작업 없이 측정합니다(조회 측정, S07-T03 NFR-03). 시드는 측정 전에 한 번만 넣고 회차마다 비우지 않습니다
    /// (회차마다 <c>Database.ResetAsync</c>를 부르면 시드가 지워짐).
    /// </summary>
    /// <param name="warmupCount">워밍업 횟수(0 이상).</param>
    /// <param name="iterationCount">측정 횟수(1 이상).</param>
    /// <param name="action">잴 작업.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>워밍업 · 측정 시간.</returns>
    /// <exception cref="ArgumentOutOfRangeException">횟수가 범위 밖인 경우.</exception>
    public static Task<MeasurementResult> MeasureAsync(
        int warmupCount,
        int iterationCount,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken) =>
        MeasureAsync(warmupCount, iterationCount, static _ => Task.CompletedTask, action, cancellationToken);

    private static async Task<TimeSpan> RunOnceAsync(Func<CancellationToken, Task> prepare, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await prepare(cancellationToken);

        var started = Stopwatch.GetTimestamp();
        await action(cancellationToken);
        return Stopwatch.GetElapsedTime(started);
    }
}
