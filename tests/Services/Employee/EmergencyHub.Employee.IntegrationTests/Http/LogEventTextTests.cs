using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using Serilog.Events;
using Serilog.Parsing;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S07-T03 도구(developer, BL-139): UnexpectedErrors 제외를 이벤트 ID + SourceContext로. 이벤트 ID만 받는 기존 오버로드는 그대로 둔다.
public sealed class LogEventTextTests
{
    private const string RequestSizeLimitFilter = "Microsoft.AspNetCore.Mvc.Filters.RequestSizeLimitFilter";

    // ---- 성공 ----

    [Fact]
    public void UnexpectedErrors_GlobalExceptionExpected_ExcludesOnlyThatCategory()
    {
        LogEvent[] events =
        [
            NewEvent(LogEventLevel.Error, 1, ExpectedLogEvent.GlobalException.SourceContext),
            NewEvent(LogEventLevel.Error, 1, RequestSizeLimitFilter),
        ];

        events.UnexpectedErrors(ExpectedLogEvent.GlobalException).Should().ContainSingle()
            .Which.SourceContext().Should().Be(RequestSizeLimitFilter, "같은 ID 1이라도 범주가 다르면 판정받는다");
        events.UnexpectedErrors(1).Should().BeEmpty("기존 ID만 받는 오버로드는 동작이 그대로다(호출처 호환)");
    }

    [Fact]
    public void GlobalException_SourceContext_IsExistingHandlerTypeName()
    {
        var handler = typeof(ApiServiceCollectionExtensions).Assembly.GetType(ExpectedLogEvent.GlobalException.SourceContext);

        handler.Should().NotBeNull("범주 문자열은 BuildingBlocks.Api 전역 예외 처리기 형식 이름과 같아야 한다(internal이라 typeof 대신 이름 대조)");
        handler!.Name.Should().Be("GlobalExceptionHandler");
    }

    // ---- 실패 ----

    [Fact]
    public void UnexpectedErrors_SameCategoryDifferentId_IsKept()
    {
        LogEvent[] events = [NewEvent(LogEventLevel.Fatal, 2, ExpectedLogEvent.GlobalException.SourceContext)];

        events.UnexpectedErrors(ExpectedLogEvent.GlobalException).Should().ContainSingle("범주가 같아도 ID가 다르면 판정받는다");
    }

    // ---- 엣지 ----

    [Fact]
    public void UnexpectedErrors_WarningMissingIdAndSeveralExpected_KeepsOnlyErrorWithoutMatch()
    {
        var other = new ExpectedLogEvent(1, RequestSizeLimitFilter);
        LogEvent[] events =
        [
            NewEvent(LogEventLevel.Warning, 7, "Any"),
            NewEvent(LogEventLevel.Error, null, ExpectedLogEvent.GlobalException.SourceContext),
            NewEvent(LogEventLevel.Error, 1, ExpectedLogEvent.GlobalException.SourceContext),
            NewEvent(LogEventLevel.Error, 1, RequestSizeLimitFilter),
        ];

        var unexpected = events.UnexpectedErrors(ExpectedLogEvent.GlobalException, other);

        unexpected.Should().ContainSingle("Warning 이하 · 제외 두 건이 빠지고 ID 없는 Error만 남는다").Which.EventId().Should().BeNull();
    }

    private static LogEvent NewEvent(LogEventLevel level, int? eventId, string sourceContext)
    {
        List<LogEventProperty> properties = [new("SourceContext", new ScalarValue(sourceContext))];
        if (eventId is { } id)
        {
            properties.Add(new LogEventProperty("EventId", new StructureValue([new LogEventProperty("Id", new ScalarValue(id))])));
        }

        return new LogEvent(DateTimeOffset.UnixEpoch, level, exception: null, new MessageTemplateParser().Parse("Test"), properties);
    }
}
