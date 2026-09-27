using System.Diagnostics;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Api.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Exceptions;

// 전역 예외 처리기(ADR-0024 "Infrastructure 예외 분류 경로", S02-T06 완료 조건).
// 분류기를 차례로 묻고 모두 null이면 9001, 분류기 0개여도 동작. 응답 · 로그에 예외 메시지 · 스택 · 제약 이름 · SQL 없음.
// 이벤트 ID 1(Error)은 이 처리기에서만 쓴다.
public sealed class GlobalExceptionHandlerTests
{
    private readonly FakeLogger<GlobalExceptionHandler> _logger = new();

    public static TheoryData<string> UnconvertedDbScenarios() => new("23514", "25006");

    // ---- 성공 ----

    [Fact]
    public async Task TryHandleAsync_NoClassifiers_Responds500With9001()
    {
        // 엣지이자 기본 경로: Infrastructure를 쓰지 않는 호스트는 분류기가 0개다.
        var context = HttpContexts.Create();

        var handled = await CreateHandler().TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().StartWith(ErrorProblemDetails.ContentType);
        var json = HttpContexts.ReadJson(context);
        json.GetProperty("code").GetInt32().Should().Be(9001);
        json.GetProperty("detail").GetString().Should().Be(CommonErrors.Unexpected.Message);
        json.GetProperty("traceId").GetString().Should().Be(HttpContexts.TraceIdentifier);
    }

    [Fact]
    public async Task TryHandleAsync_ClassifierReturnsError_RespondsWithThatErrorAndLogsWarning301()
    {
        // RetryLimitExceededException → 9003은 Infrastructure 분류기의 결과다(타입 이름 문자열 판별 없음).
        var classifier = Substitute.For<IExceptionClassifier>();
        var exception = new TimeoutException("retry limit");
        classifier.Classify(exception).Returns(CommonErrors.TemporarilyUnavailable);
        var context = HttpContexts.Create();

        await CreateHandler(classifier).TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        HttpContexts.ReadJson(context).GetProperty("code").GetInt32().Should().Be(9003);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(301);
        record.Level.Should().Be(LogLevel.Warning);
        record.StructuredState.Should().Contain(new KeyValuePair<string, string?>("ErrorCode", "9003"));
    }

    [Fact]
    public async Task TryHandleAsync_SeveralClassifiers_FirstNonNullWinsAndLaterAreNotAsked()
    {
        var first = Substitute.For<IExceptionClassifier>();
        var second = Substitute.For<IExceptionClassifier>();
        var third = Substitute.For<IExceptionClassifier>();
        first.Classify(Arg.Any<Exception>()).Returns((Error?)null);
        second.Classify(Arg.Any<Exception>()).Returns(CommonErrors.ExternalServiceFailed);
        var context = HttpContexts.Create();

        await CreateHandler(first, second, third).TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        Received.InOrder(() =>
        {
            first.Classify(Arg.Any<Exception>());
            second.Classify(Arg.Any<Exception>());
        });
        third.DidNotReceive().Classify(Arg.Any<Exception>());
    }

    [Fact]
    public async Task TryHandleAsync_Unclassified_LogsEventId1AtErrorOnceWithRedactedException()
    {
        var exception = new InvalidOperationException("secret detail");

        await CreateHandler().TryHandleAsync(HttpContexts.Create(), exception, CancellationToken.None);

        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(1);
        record.Level.Should().Be(LogLevel.Error);
        record.Exception.Should().BeOfType<RedactedException>().Which.OriginalTypeName.Should().Be(typeof(InvalidOperationException).FullName);
        record.StructuredState.Should().Contain(new KeyValuePair<string, string?>("ExceptionType", typeof(InvalidOperationException).FullName));
        record.StructuredState.Should().Contain(new KeyValuePair<string, string?>("ErrorCode", "9001"));
    }

    [Fact]
    public async Task TryHandleAsync_W3CActivity_RespondsWithActivityTraceId()
    {
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
        var context = HttpContexts.Create();

        await CreateHandler().TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        HttpContexts.ReadJson(context).GetProperty("traceId").GetString().Should().Be(activity.TraceId.ToHexString());
    }

    // ---- 실패 ----

    [Fact]
    public async Task TryHandleAsync_AllClassifiersReturnNull_Responds9001()
    {
        var first = Substitute.For<IExceptionClassifier>();
        var second = Substitute.For<IExceptionClassifier>();
        first.Classify(Arg.Any<Exception>()).Returns((Error?)null);
        second.Classify(Arg.Any<Exception>()).Returns((Error?)null);
        var context = HttpContexts.Create();

        await CreateHandler(first, second).TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        HttpContexts.ReadJson(context).GetProperty("code").GetInt32().Should().Be(9001);
        first.Received(1).Classify(Arg.Any<Exception>());
        second.Received(1).Classify(Arg.Any<Exception>());
    }

    [Theory]
    [MemberData(nameof(UnconvertedDbScenarios))]
    public async Task TryHandleAsync_UnconvertedDbException_Responds9001WithoutConstraintNameSqlOrStack(string sqlState)
    {
        // 23514 · 25006은 Infrastructure가 변환하지 않는다(S02-T07). 일반 예외와 같이 9001이다.
        var exception = sqlState == "23514" ? UnconvertedDbFailures.CheckViolation() : UnconvertedDbFailures.ReadOnlyViolation();
        var context = HttpContexts.Create();

        await CreateHandler().TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var body = HttpContexts.ReadBody(context);
        HttpContexts.ReadJson(context).GetProperty("code").GetInt32().Should().Be(9001);
        body.Should().NotContain(" at ").And.NotContain(nameof(UnconvertedDbFailures));
        foreach (var fragment in UnconvertedDbFailures.SensitiveFragments)
        {
            body.Should().NotContain(fragment);
        }
    }

    [Theory]
    [MemberData(nameof(UnconvertedDbScenarios))]
    public async Task TryHandleAsync_UnconvertedDbException_LogHasNoConstraintNameSqlOrValue(string sqlState)
    {
        var exception = sqlState == "23514" ? UnconvertedDbFailures.CheckViolation() : UnconvertedDbFailures.ReadOnlyViolation();

        await CreateHandler().TryHandleAsync(HttpContexts.Create(), exception, CancellationToken.None);

        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(1);
        var rendered = string.Join(
            '\n',
            record.Message,
            record.Exception!.ToString(),
            string.Join('\n', record.StructuredState!.Select(pair => $"{pair.Key}={pair.Value}")));
        foreach (var fragment in UnconvertedDbFailures.SensitiveFragments)
        {
            rendered.Should().NotContain(fragment);
        }

        // 원인 추적용 형식 이름과 스택은 남는다.
        rendered.Should().Contain(typeof(SampleDbException).FullName).And.Contain(nameof(UnconvertedDbFailures));
    }

    [Fact]
    public async Task TryHandleAsync_BadHttpRequest_Responds400With1001WithoutAskingClassifiers()
    {
        var classifier = Substitute.For<IExceptionClassifier>();
        var context = HttpContexts.Create();

        await CreateHandler(classifier).TryHandleAsync(
            context,
            new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge),
            CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        HttpContexts.ReadJson(context).GetProperty("code").GetInt32().Should().Be(1001);
        classifier.DidNotReceive().Classify(Arg.Any<Exception>());
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(302);
        record.Level.Should().Be(LogLevel.Information);
        record.Exception.Should().BeNull();
    }

    [Fact]
    public async Task TryHandleAsync_ResponseAlreadyStarted_ReturnsFalseWithoutWriting()
    {
        var context = HttpContexts.Create();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var handled = await CreateHandler().TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        handled.Should().BeFalse();
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task TryHandleAsync_NullArguments_ThrowArgumentNullException()
    {
        var handler = CreateHandler();

        var nullContext = () => handler.TryHandleAsync(null!, new InvalidOperationException(), CancellationToken.None).AsTask();
        var nullException = () => handler.TryHandleAsync(HttpContexts.Create(), null!, CancellationToken.None).AsTask();

        await nullContext.Should().ThrowAsync<ArgumentNullException>().WithParameterName("httpContext");
        await nullException.Should().ThrowAsync<ArgumentNullException>().WithParameterName("exception");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task TryHandleAsync_RequestAbortedByClient_LowersLogLevelOnlyAndDoesNotThrowWhileWriting()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var context = HttpContexts.Create();
        context.RequestAborted = aborted.Token;

        var handled = await CreateHandler().TryHandleAsync(context, new OperationCanceledException(aborted.Token), aborted.Token);

        // 판정 · 상태 코드는 9001(500) 그대로다. 본문은 끊긴 연결이라 프레임워크가 쓰기를 건너뛴다(RequestAborted 기준, 예외 없음).
        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(303);
        record.StructuredState.Should().Contain(new KeyValuePair<string, string?>("ErrorCode", "9001"));
        record.Level.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task TryHandleAsync_IOExceptionAfterClientAbort_IsTreatedAsAbort()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var context = HttpContexts.Create();
        context.RequestAborted = aborted.Token;

        await CreateHandler().TryHandleAsync(context, new IOException("connection reset"), CancellationToken.None);

        _logger.Collector.GetSnapshot().Should().ContainSingle().Which.Id.Id.Should().Be(303);
    }

    [Fact]
    public async Task TryHandleAsync_CanceledWithoutClientAbort_IsUnexpectedError()
    {
        // 요청이 중단되지 않았는데 난 취소(내부 시간 초과 등)는 예상 못한 오류다.
        await CreateHandler().TryHandleAsync(HttpContexts.Create(), new OperationCanceledException(), CancellationToken.None);

        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(1);
        record.Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task TryHandleAsync_ClientAbortWithOtherException_IsStillUnexpectedError()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var context = HttpContexts.Create();
        context.RequestAborted = aborted.Token;

        await CreateHandler().TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        _logger.Collector.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task TryHandleAsync_ClassifierReturnsValidationError_WritesErrorsExtension()
    {
        var classifier = Substitute.For<IExceptionClassifier>();
        classifier.Classify(Arg.Any<Exception>()).Returns(ValidationError.Create([FieldError.Create("Email", SampleErrors.InvalidEmail)]));
        var context = HttpContexts.Create();

        await CreateHandler(classifier).TryHandleAsync(context, new FormatException(), CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        HttpContexts.ReadJson(context).GetProperty("errors").GetProperty("email")[0].GetProperty("code").GetInt32().Should().Be(21001);
    }

    private GlobalExceptionHandler CreateHandler(params IExceptionClassifier[] classifiers) =>
        new(classifiers, Options.Create(new JsonOptions()), _logger);
}
