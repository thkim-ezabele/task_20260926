using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Pipeline;

// 개인정보 없음 단언(PersonalData.ShouldNotContain)이 공허하게 통과하지 않는지 확인한다.
public sealed class PersonalDataAssertionTests
{
    [Fact]
    public void ShouldNotContain_ValueInStructuredState_Fails()
    {
        var logger = new FakeLogger();
        logger.Log(LogLevel.Information, new EventId(1), new List<KeyValuePair<string, object?>> { new("Email", PersonalData.Email) }, null, (_, _) => "no value");

        var act = () => PersonalData.ShouldNotContain(logger.Collector.GetSnapshot());

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ShouldNotContain_ValueInMessage_Fails()
    {
        var logger = new FakeLogger();
        logger.Log(LogLevel.Information, new EventId(1), new List<KeyValuePair<string, object?>>(), null, (_, _) => $"registered {PersonalData.Name}");

        var act = () => PersonalData.ShouldNotContain(logger.Collector.GetSnapshot());

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ShouldNotContain_ExtraForbiddenValue_Fails()
    {
        var logger = new FakeLogger();
        logger.Log(LogLevel.Information, new EventId(1), new List<KeyValuePair<string, object?>> { new("Id", "abc-123") }, null, (_, _) => "ok");

        var act = () => PersonalData.ShouldNotContain(logger.Collector.GetSnapshot(), "abc-123");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ShouldNotContain_NoForbiddenValue_Passes()
    {
        var logger = new FakeLogger();
        logger.Log(LogLevel.Information, new EventId(1), new List<KeyValuePair<string, object?>> { new("ErrorCode", 1001) }, null, (_, _) => "failed 1001");

        var act = () => PersonalData.ShouldNotContain(logger.Collector.GetSnapshot());

        act.Should().NotThrow();
    }
}
