using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>
/// 테스트 요청 · 응답에 넣는 개인정보 값과, 로그 기록에 그 값이 없는지 확인하는 단언 도우미.
/// </summary>
public static class PersonalData
{
    public const string Name = "홍길동";

    public const string Email = "hong.gildong@example.com";

    public static IReadOnlyList<string> Values { get; } = [Name, Email];

    /// <summary>
    /// 기록의 메시지 · 구조화 속성 값 · 범주 어디에도 <paramref name="forbidden"/> 값이 없는지 확인합니다.
    /// </summary>
    public static void ShouldNotContain(IEnumerable<FakeLogRecord> records, params string[] forbidden)
    {
        string[] values = [.. Values, .. forbidden];

        foreach (var record in records)
        {
            var texts = new List<string> { record.Message, record.Category ?? string.Empty };
            texts.AddRange((record.StructuredState ?? []).Select(pair => $"{pair.Key}={pair.Value}"));

            foreach (var value in values)
            {
                texts.Should().NotContain(text => text.Contains(value, StringComparison.Ordinal), "로그에 개인정보 · 요청 · 응답 값({0})을 남기지 않는다", value);
            }
        }
    }
}
