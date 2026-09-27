using System.Reflection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// 로그 이벤트 ID 하위 범위(BL-028): 영속성은 201 ~ 299. 원본은 wiki/05-api/error-codes.md "영속성 로그 이벤트" 표.
// 표를 바꾸면 이 데이터도 함께 바꾼다.
public sealed class PersistenceLogsTests
{
    private static readonly IReadOnlyList<(string Name, LoggerMessageAttribute Definition)> Definitions = typeof(PersistenceLogs)
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Select(method => (method.Name, Definition: method.GetCustomAttribute<LoggerMessageAttribute>()))
        .Where(pair => pair.Definition is not null)
        .Select(pair => (pair.Name, pair.Definition!))
        .ToList();

    [Fact]
    public void Definitions_AreTheThreeDocumentedEvents()
    {
        Definitions.Select(pair => (pair.Definition.EventId, pair.Name, pair.Definition.Level, pair.Definition.Message))
            .Should().BeEquivalentTo(new[]
            {
                (201, "UniqueConstraintViolationMapped", LogLevel.Debug,
                    "Unique constraint {ConstraintName} violated with SqlState {SqlState} on {EntityTypes}, returned error {ErrorCode}"),
                (202, "UniqueConstraintViolationUnmapped", LogLevel.Warning,
                    "Unique constraint {ConstraintName} violated with SqlState {SqlState} on {EntityTypes} has no error mapping, returned error {ErrorCode}"),
                (203, "ConcurrencyConflictDetected", LogLevel.Debug,
                    "Concurrency conflict on {EntityTypes}, returned error {ErrorCode}"),
            });
    }

    [Fact]
    public void EventIds_AreInsidePersistenceRange201To299AndUnique()
    {
        Definitions.Should().NotBeEmpty();
        Definitions.Should().OnlyContain(pair => pair.Definition.EventId >= 201 && pair.Definition.EventId <= 299);
        Definitions.Select(pair => pair.Definition.EventId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Definitions_NeverUseInformationOrErrorLevel()
    {
        // 실패 Result는 로깅 데코레이터가 102(Information)로 이미 남긴다. 변환 로그는 Information을 다시 쓰지 않고, 예외가 아니므로 Error도 아니다.
        Definitions.Should().OnlyContain(pair => pair.Definition.Level == LogLevel.Debug || pair.Definition.Level == LogLevel.Warning);
    }

    [Fact]
    public void Templates_OnlyUseAllowedProperties()
    {
        // 속성은 제약 이름 · SqlState · 엔티티 형식 · 에러 코드뿐이다(Detail · MessageText · 값 · 키 금지, database.md "변환 로그와 메시지").
        string[] allowed = ["{ConstraintName}", "{SqlState}", "{EntityTypes}", "{ErrorCode}"];

        var placeholders = Definitions
            .SelectMany(pair => System.Text.RegularExpressions.Regex.Matches(pair.Definition.Message!, @"\{[^}]+\}").Select(match => match.Value))
            .Distinct()
            .ToList();

        placeholders.Should().NotBeEmpty();
        placeholders.Should().BeSubsetOf(allowed);
    }
}
