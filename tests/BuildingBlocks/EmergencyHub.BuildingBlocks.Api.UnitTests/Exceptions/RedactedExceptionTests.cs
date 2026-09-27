using EmergencyHub.BuildingBlocks.Api.Exceptions;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Exceptions;

// 로그용 예외 사본: 형식 이름 · 스택은 남기고 메시지(제약 이름 · SQL · 값이 들어갈 수 있음)는 뺀다(ADR-0024, ADR-0020).
public sealed class RedactedExceptionTests
{
    // ---- 성공 ----

    [Fact]
    public void From_ThrownException_KeepsTypeNameAndStackTraceWithoutMessage()
    {
        var original = UnconvertedDbFailures.ReadOnlyViolation();

        var redacted = RedactedException.From(original);

        redacted.OriginalTypeName.Should().Be(typeof(SampleDbException).FullName);
        redacted.Message.Should().Contain(typeof(SampleDbException).FullName).And.NotContain("25006");
        redacted.StackTrace.Should().Be(original.StackTrace);
        redacted.StackTrace.Should().Contain(nameof(UnconvertedDbFailures));
    }

    [Fact]
    public void From_InnerExceptionChain_IsRedactedRecursively()
    {
        var original = UnconvertedDbFailures.CheckViolation();

        var redacted = RedactedException.From(original);

        redacted.OriginalTypeName.Should().Be(typeof(InvalidOperationException).FullName);
        var inner = redacted.InnerException.Should().BeOfType<RedactedException>().Subject;
        inner.OriginalTypeName.Should().Be(typeof(SampleDbException).FullName);
        inner.InnerException.Should().BeNull();
    }

    [Fact]
    public void ToString_ContainsTypesAndStackButNoSensitiveFragment()
    {
        var text = RedactedException.From(UnconvertedDbFailures.CheckViolation()).ToString();

        text.Should().Contain(typeof(InvalidOperationException).FullName).And.Contain(typeof(SampleDbException).FullName);
        foreach (var fragment in UnconvertedDbFailures.SensitiveFragments)
        {
            text.Should().NotContain(fragment);
        }
    }

    // ---- 실패 ----

    [Fact]
    public void From_Null_ThrowsArgumentNullException()
    {
        var act = () => RedactedException.From(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }

    // ---- 엣지 ----

    [Fact]
    public void From_NeverThrownException_HasNoStackTrace()
    {
        var redacted = RedactedException.From(new InvalidOperationException("never thrown"));

        redacted.StackTrace.Should().BeNull();
        redacted.ToString().Should().NotContain("never thrown");
    }

    [Fact]
    public void From_AggregateException_KeepsFirstInnerOnly()
    {
        var aggregate = new AggregateException("many", new InvalidOperationException("first"), new ArgumentException("second"));

        var redacted = RedactedException.From(aggregate);

        redacted.InnerException.Should().BeOfType<RedactedException>().Which.OriginalTypeName.Should().Be(typeof(InvalidOperationException).FullName);
        redacted.ToString().Should().NotContain("first").And.NotContain("second").And.NotContain("many");
    }

    [Fact]
    public void From_DeepInnerChain_StopsAtDepthLimit()
    {
        Exception current = new InvalidOperationException("leaf");
        for (var depth = 0; depth < 100; depth++)
        {
            current = new InvalidOperationException("level", current);
        }

        var redacted = RedactedException.From(current);

        var depthCount = 0;
        for (Exception? cursor = redacted; cursor is not null; cursor = cursor.InnerException)
        {
            depthCount++;
        }

        depthCount.Should().Be(RedactedException.MaxDepth);
    }
}
