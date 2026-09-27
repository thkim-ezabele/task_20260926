using System.Reflection;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests;

// 로그 이벤트 ID(BL-028): 1은 전역 예외 처리기 전용, API 공통 처리는 301 ~ 399. 원본은 wiki/05-api/error-codes.md "API 로그 이벤트".
public sealed class ApiLogsTests
{
    private static readonly IReadOnlyList<LoggerMessageAttribute> Definitions = typeof(ApiLogs)
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Select(method => method.GetCustomAttribute<LoggerMessageAttribute>())
        .OfType<LoggerMessageAttribute>()
        .ToList();

    [Fact]
    public void Definitions_AreTheDocumentedEvents()
    {
        // error-codes.md 표와 1:1로 대조한다. 추가하면 이 표와 문서를 함께 갱신한다.
        Definitions.Select(definition => (definition.EventId, definition.Level, definition.Message)).Should().BeEquivalentTo(new[]
        {
            (1, LogLevel.Error, "Unhandled exception {ExceptionType} while processing {RequestMethod} request, returned error {ErrorCode}"),
            (301, LogLevel.Warning, "Exception {ExceptionType} classified as error {ErrorCode} of type {ErrorType}"),
            (302, LogLevel.Information, "Bad HTTP request rejected with status {StatusCode}, returned error {ErrorCode}"),
            (303, LogLevel.Information, "Request aborted by client with exception {ExceptionType}, returned error {ErrorCode}"),
            (304, LogLevel.Debug, "Request model binding failed for fields {FieldNames}, returned error {ErrorCode}"),
        });
    }

    [Fact]
    public void EventIds_AreOneOrInsideApiRange301To399()
    {
        Definitions.Should().NotBeEmpty();
        Definitions.Should().OnlyContain(definition => definition.EventId == 1 || (definition.EventId >= 301 && definition.EventId <= 399));
    }

    [Fact]
    public void EventIds_AreUnique()
    {
        Definitions.Select(definition => definition.EventId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void OnlyEventId1_UsesErrorLevel()
    {
        // 예외로 끝난 요청 중 예상하지 못한 것만 Error다. 분류된 예외 · 잘못된 요청 · 요청 중단은 낮은 수준이다.
        Definitions.Where(definition => definition.Level >= LogLevel.Error).Select(definition => definition.EventId).Should().Equal(1);
    }
}
