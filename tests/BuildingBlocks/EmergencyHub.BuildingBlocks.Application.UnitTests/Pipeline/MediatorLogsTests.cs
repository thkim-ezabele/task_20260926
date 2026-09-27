using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Pipeline;

// 로그 이벤트 ID 하위 범위(BL-028): Mediator는 101 ~ 199. 원본은 wiki/05-api/error-codes.md "로그 이벤트 ID 범위".
public sealed class MediatorLogsTests
{
    private static readonly IReadOnlyList<LoggerMessageAttribute> Definitions = typeof(MediatorLogs)
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Select(method => method.GetCustomAttribute<LoggerMessageAttribute>())
        .OfType<LoggerMessageAttribute>()
        .ToList();

    [Fact]
    public void Definitions_AreTheFourDocumentedEvents()
    {
        // error-codes.md 표와 1:1로 대조한다. 추가하면 이 표와 문서를 함께 갱신한다.
        Definitions.Select(definition => (definition.EventId, definition.Level)).Should().BeEquivalentTo(new[]
        {
            (101, LogLevel.Debug),
            (102, LogLevel.Information),
            (103, LogLevel.Debug),
            (104, LogLevel.Information),
        });
    }

    [Fact]
    public void EventIds_AreInsideMediatorRange101To199()
    {
        Definitions.Should().NotBeEmpty();
        Definitions.Should().OnlyContain(definition => definition.EventId >= 101 && definition.EventId <= 199);
    }

    [Fact]
    public void EventIds_AreUnique()
    {
        Definitions.Select(definition => definition.EventId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Definitions_NeverUseErrorOrHigherLevel()
    {
        // 예상 가능한 실패는 Error가 아니고 예외는 이 데코레이터에서 기록하지 않는다(ADR-0015).
        Definitions.Should().OnlyContain(definition => definition.Level < LogLevel.Warning);
    }
}
