using Serilog.Events;
using Serilog.Parsing;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S06-T06 도구(developer): 응답 본문 · 로그의 개인정보 값 검색과 판정 제외 도우미. 400 / 409 / 500 본 테스트는 tester가 쓴다.
public sealed class PersonalDataScanTests
{
    private static readonly string[] Values = ["김이름", "kim@gmail.com", "010-0000-0000"];

    // ---- 성공 ----

    [Fact]
    public void FindInJson_EscapedKoreanAndUppercaseEmail_FindsDecodedValues()
    {
        // System.Text.Json 기본 인코더(ProblemDetails 직렬화와 같음)는 한글을 이스케이프한다(원문 검색만으로는 못 찾음).
        var json = System.Text.Json.JsonSerializer.Serialize(new { detail = "김이름", errors = new { email = "KIM@GMAIL.COM" } });

        PersonalDataScan.FindIn(json, ["김이름"]).Should().BeEmpty("원문에는 이스케이프된 형태만 있다");
        PersonalDataScan.FindInJson(json, Values).Should().Equal("김이름", "kim@gmail.com");
    }

    [Fact]
    public void FindInLogs_ValueInNestedProperty_ReportsEventIdAndValue()
    {
        var logEvent = NewEvent(
            LogEventLevel.Warning,
            eventId: 1,
            new LogEventProperty("Payload", new StructureValue([new LogEventProperty("Tel", new ScalarValue("010-0000-0000"))])));

        var exposures = PersonalDataScan.FindInLogs([logEvent], Values);

        exposures.Should().ContainSingle().Which.Should().Be(new LogExposure(1, LogEventLevel.Warning, "Test", "Test {Payload}", "010-0000-0000"));
    }

    // ---- 실패 ----

    [Fact]
    public void FindInLogs_OnlyIdsAndCodes_ReturnsEmpty()
    {
        var logEvent = NewEvent(LogEventLevel.Information, eventId: 20001, new LogEventProperty("Payload", new ScalarValue(Guid.Empty)));

        PersonalDataScan.FindInLogs([logEvent], Values).Should().BeEmpty();
    }

    [Fact]
    public void FindInJson_InvalidJson_Throws()
    {
        var act = () => PersonalDataScan.FindInJson("not json", Values);

        act.Should().Throw<System.Text.Json.JsonException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void UnexpectedErrors_ExpectedEventIdExcluded_OtherErrorsAndMissingIdsKept()
    {
        LogEvent[] events =
        [
            NewEvent(LogEventLevel.Error, eventId: 1, new LogEventProperty("Payload", new ScalarValue(1))),
            NewEvent(LogEventLevel.Error, eventId: 202, new LogEventProperty("Payload", new ScalarValue(2))),
            NewEvent(LogEventLevel.Fatal, eventId: null, new LogEventProperty("Payload", new ScalarValue(3))),
            NewEvent(LogEventLevel.Warning, eventId: 301, new LogEventProperty("Payload", new ScalarValue(4))),
        ];

        var unexpected = events.UnexpectedErrors(1);

        unexpected.Select(logEvent => logEvent.EventId()).Should().Equal([202, null], "Warning 이하와 단언한 ID(1)만 빠지고 ID 없는 Fatal은 남는다");
        PersonalDataScan.FindIn("x", ["", "x"]).Should().Equal(["x"], "빈 값은 검색하지 않는다");
    }

    private static LogEvent NewEvent(LogEventLevel level, int? eventId, LogEventProperty payload)
    {
        List<LogEventProperty> properties = [payload, new("SourceContext", new ScalarValue("Test"))];
        if (eventId is { } id)
        {
            properties.Add(new LogEventProperty("EventId", new StructureValue([new LogEventProperty("Id", new ScalarValue(id))])));
        }

        return new LogEvent(DateTimeOffset.UnixEpoch, level, exception: null, new MessageTemplateParser().Parse("Test {Payload}"), properties);
    }
}
