namespace EmergencyHub.Employee.IntegrationTests.Performance;

/// <summary>반복 측정 결과입니다(<see cref="PerformanceMeasurement.MeasureAsync"/>).</summary>
/// <param name="Warmups">워밍업 실행 시간(판정에 쓰지 않음).</param>
/// <param name="Samples">측정 실행 시간(실행 순서, 1개 이상).</param>
public sealed record MeasurementResult(IReadOnlyList<TimeSpan> Warmups, IReadOnlyList<TimeSpan> Samples)
{
    /// <summary>측정값의 중앙값입니다(개수가 짝수면 가운데 두 값의 평균).</summary>
    /// <exception cref="InvalidOperationException">측정값이 없는 경우.</exception>
    public TimeSpan Median
    {
        get
        {
            if (Samples.Count == 0)
            {
                throw new InvalidOperationException("측정값이 없습니다.");
            }

            var sorted = Samples.Order().ToArray();
            var middle = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
        }
    }

    /// <summary>측정값의 최댓값입니다.</summary>
    public TimeSpan Max => Samples.Max();

    /// <summary>진행 기록용 한 줄 요약(밀리초, 소수 1자리)입니다.</summary>
    /// <returns>예: <c>median=812.3ms samples=[800.1, 812.3, 830.0] warmups=[1500.2]</c>.</returns>
    public string Describe() =>
        FormattableString.Invariant(
            $"median={Median.TotalMilliseconds:0.0}ms samples=[{string.Join(", ", Samples.Select(Milliseconds))}] warmups=[{string.Join(", ", Warmups.Select(Milliseconds))}]");

    private static string Milliseconds(TimeSpan value) => FormattableString.Invariant($"{value.TotalMilliseconds:0.0}");
}
