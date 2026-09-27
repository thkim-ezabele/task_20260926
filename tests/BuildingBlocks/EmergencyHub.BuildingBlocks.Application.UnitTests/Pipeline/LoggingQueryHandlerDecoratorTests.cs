using System.Globalization;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Pipeline;

// Query 로깅 데코레이터. Command와 같은 규칙이고 이벤트 ID만 다르다(103 성공 · 104 실패).
public sealed class LoggingQueryHandlerDecoratorTests
{
    private readonly IQueryHandler<FindPersonQuery, SampleResponse> _inner = Substitute.For<IQueryHandler<FindPersonQuery, SampleResponse>>();
    private readonly FakeLogger<LoggingQueryHandlerDecorator<FindPersonQuery, SampleResponse>> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly FindPersonQuery _query = new(PersonalData.Email);

    // ---- 성공 ----

    [Fact]
    public async Task Handle_InnerSucceeds_ReturnsSameResultAndLogsOneDebugRecordWithEventId103()
    {
        var innerResult = Result.Success(new SampleResponse(7, PersonalData.Name));
        _inner.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(15));
            return innerResult;
        });

        var result = await CreateSut().Handle(_query, CancellationToken.None);

        result.Should().BeSameAs(innerResult);
        await _inner.Received(1).Handle(Arg.Is<FindPersonQuery>(received => ReferenceEquals(received, _query)), CancellationToken.None);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Debug);
        record.Id.Id.Should().Be(103);
        record.Id.Name.Should().Be("QuerySucceeded");
        record.GetStructuredStateValue("RequestName").Should().Be(nameof(FindPersonQuery));
        record.GetStructuredStateValue("ElapsedMilliseconds").Should().Be("15");
        record.GetStructuredStateValue("{OriginalFormat}").Should().Be("Query {RequestName} succeeded in {ElapsedMilliseconds} ms");
    }

    // ---- 실패 Result ----

    [Fact]
    public async Task Handle_InnerReturnsFailure_ReturnsSameResultAndLogsOneInformationRecordWithEventId104()
    {
        var innerResult = Result.Failure<SampleResponse>(CommonErrors.NotFound);
        _inner.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(innerResult);

        var result = await CreateSut().Handle(_query, CancellationToken.None);

        result.Should().BeSameAs(innerResult);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Information);
        record.Id.Id.Should().Be(104);
        record.Id.Name.Should().Be("QueryFailed");
        record.GetStructuredStateValue("ErrorCode").Should().Be("2001");
        record.GetStructuredStateValue("ErrorType").Should().Be(((short)ErrorType.NotFound).ToString(CultureInfo.InvariantCulture));
        record.GetStructuredStateValue("{OriginalFormat}").Should().Be(
            "Query {RequestName} failed with error {ErrorCode} of type {ErrorType} in {ElapsedMilliseconds} ms");
    }

    // ---- 예외 ----

    [Fact]
    public async Task Handle_InnerThrows_PropagatesSameExceptionAndWritesNoLog()
    {
        var exception = new InvalidOperationException("예상하지 못한 오류");
        _inner.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Result<SampleResponse>>(exception));

        var act = () => CreateSut().Handle(_query, CancellationToken.None);

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        _logger.Collector.Count.Should().Be(0);
    }

    // ---- 개인정보 · 엣지 ----

    [Fact]
    public async Task Handle_PersonalDataInQueryAndResponse_DoesNotLogValues()
    {
        _inner.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new SampleResponse(7, PersonalData.Name)), Result.Failure<SampleResponse>(CommonErrors.NotFound));
        var sut = CreateSut();

        await sut.Handle(_query, CancellationToken.None);
        await sut.Handle(_query, CancellationToken.None);

        _logger.Collector.Count.Should().Be(2);
        PersonalData.ShouldNotContain(_logger.Collector.GetSnapshot(), CommonErrors.NotFound.Message);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesSameTokenToInner()
    {
        using var cancellation = new CancellationTokenSource();
        _inner.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new SampleResponse(1, "샘플")));

        await CreateSut().Handle(_query, cancellation.Token);

        await _inner.Received(1).Handle(_query, cancellation.Token);
    }

    [Fact]
    public void Decorator_ImplementsOnlyQueryHandler()
    {
        typeof(LoggingQueryHandlerDecorator<,>).GetInterfaces().Should().ContainSingle()
            .Which.GetGenericTypeDefinition().Should().Be(typeof(IQueryHandler<,>));
    }

    private LoggingQueryHandlerDecorator<FindPersonQuery, SampleResponse> CreateSut() => new(_inner, _logger, _timeProvider);
}
