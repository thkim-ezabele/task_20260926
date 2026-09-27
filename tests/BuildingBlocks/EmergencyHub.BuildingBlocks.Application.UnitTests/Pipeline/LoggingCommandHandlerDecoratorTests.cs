using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Pipeline;

// ADR-0015 로깅 데코레이터(가장 바깥): 요청 형식 이름, 결과(성공 / 실패 코드 · ErrorType), 경과 시간만 남긴다.
// 성공 Debug · 실패 Result Information, 예외는 잡지도 기록하지도 않는다(예외 로그는 전역 예외 처리기 한 곳).
public sealed class LoggingCommandHandlerDecoratorTests
{
    private readonly ICommandHandler<RegisterPersonCommand, Guid> _inner = Substitute.For<ICommandHandler<RegisterPersonCommand, Guid>>();
    private readonly FakeLogger<LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid>> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly RegisterPersonCommand _command = new(PersonalData.Name, PersonalData.Email);

    public static TheoryData<Error> FailureErrors() =>
        new(
            CommonErrors.NotFound,
            CommonErrors.ConcurrencyConflict,
            Error.BusinessRule(24001, "허용되지 않은 상태 전이입니다."),
            CommonErrors.TemporarilyUnavailable,
            ValidationError.Create([FieldError.Create("Email", CommonErrors.ValidationFailed)]));

    // ---- 성공 ----

    [Fact]
    public async Task Handle_InnerSucceeds_ReturnsSameResultAndLogsOneDebugRecordWithEventId101()
    {
        var innerResult = Result.Success(Guid.NewGuid());
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(innerResult);

        var result = await CreateSut().Handle(_command, CancellationToken.None);

        result.Should().BeSameAs(innerResult);
        await _inner.Received(1).Handle(Arg.Is<RegisterPersonCommand>(received => ReferenceEquals(received, _command)), CancellationToken.None);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Debug);
        record.Id.Id.Should().Be(101);
        record.Id.Name.Should().Be("CommandSucceeded");
        record.Exception.Should().BeNull();
        record.GetStructuredStateValue("RequestName").Should().Be(nameof(RegisterPersonCommand));
    }

    [Fact]
    public async Task Handle_InnerSucceeds_LogsElapsedMillisecondsMeasuredWithTimeProvider()
    {
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(250));
            return Result.Success(Guid.NewGuid());
        });

        await CreateSut().Handle(_command, CancellationToken.None);

        _logger.LatestRecord.GetStructuredStateValue("ElapsedMilliseconds").Should().Be("250");
    }

    [Fact]
    public async Task Handle_InnerSucceeds_UsesMessageTemplateWithOnlyAllowedProperties()
    {
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));

        await CreateSut().Handle(_command, CancellationToken.None);

        var record = _logger.LatestRecord;
        record.GetStructuredStateValue("{OriginalFormat}").Should().Be("Command {RequestName} succeeded in {ElapsedMilliseconds} ms");
        record.StructuredState!.Select(pair => pair.Key).Should().BeEquivalentTo("RequestName", "ElapsedMilliseconds", "{OriginalFormat}");
    }

    // ---- 실패 Result ----

    [Theory]
    [MemberData(nameof(FailureErrors))]
    public async Task Handle_InnerReturnsFailure_ReturnsSameResultAndLogsOneInformationRecordWithCodeAndIntegerType(Error error)
    {
        var innerResult = Result.Failure<Guid>(error);
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(innerResult);

        var result = await CreateSut().Handle(_command, CancellationToken.None);

        result.Should().BeSameAs(innerResult);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Information);
        record.Id.Id.Should().Be(102);
        record.Id.Name.Should().Be("CommandFailed");
        record.GetStructuredStateValue("RequestName").Should().Be(nameof(RegisterPersonCommand));
        record.GetStructuredStateValue("ErrorCode").Should().Be(error.Code.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // ErrorType은 이름이 아니라 정수로 남긴다(ADR-0008, logging-observability "코드값은 JSON에서도 정수").
        record.GetStructuredStateValue("ErrorType").Should().Be(((short)error.Type).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Handle_InnerReturnsFailure_DoesNotLogErrorMessageOrFieldDetails()
    {
        var error = ValidationError.Create([FieldError.Create("Email", Error.Validation(21003, "이메일 형식이 올바르지 않습니다."))]);
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<Guid>(error));

        await CreateSut().Handle(_command, CancellationToken.None);

        var record = _logger.LatestRecord;
        record.GetStructuredStateValue("{OriginalFormat}").Should().Be(
            "Command {RequestName} failed with error {ErrorCode} of type {ErrorType} in {ElapsedMilliseconds} ms");
        record.StructuredState!.Select(pair => pair.Key).Should().BeEquivalentTo(
            "RequestName", "ErrorCode", "ErrorType", "ElapsedMilliseconds", "{OriginalFormat}");
        PersonalData.ShouldNotContain([record], error.Message, error.Errors[0].Message);
    }

    [Fact]
    public async Task Handle_InnerFailsAfterDelay_LogsElapsedMilliseconds()
    {
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(2));
            return Result.Failure<Guid>(CommonErrors.ConcurrencyConflict);
        });

        await CreateSut().Handle(_command, CancellationToken.None);

        _logger.LatestRecord.GetStructuredStateValue("ElapsedMilliseconds").Should().Be("2000");
    }

    // ---- 예외: 잡지 않고 기록하지 않는다 ----

    [Fact]
    public async Task Handle_InnerThrows_PropagatesSameExceptionAndWritesNoLog()
    {
        var exception = new InvalidOperationException("예상하지 못한 오류");
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Result<Guid>>(exception));

        var act = () => CreateSut().Handle(_command, CancellationToken.None);

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        _logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InnerCanceled_PropagatesOperationCanceledAndWritesNoLog()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromCanceled<Result<Guid>>(cancellation.Token));

        var act = () => CreateSut().Handle(_command, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _logger.Collector.Count.Should().Be(0);
    }

    // ---- 개인정보 ----

    [Fact]
    public async Task Handle_SuccessWithPersonalDataInCommand_DoesNotLogCommandOrResponseValues()
    {
        var createdId = Guid.NewGuid();
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(createdId));

        await CreateSut().Handle(_command, CancellationToken.None);

        PersonalData.ShouldNotContain(_logger.Collector.GetSnapshot(), createdId.ToString());
    }

    [Fact]
    public async Task Handle_FailureWithPersonalDataInCommand_DoesNotLogCommandValues()
    {
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<Guid>(CommonErrors.NotFound));

        await CreateSut().Handle(_command, CancellationToken.None);

        PersonalData.ShouldNotContain(_logger.Collector.GetSnapshot(), CommonErrors.NotFound.Message);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Handle_WithCancellationToken_PassesSameTokenToInner()
    {
        using var cancellation = new CancellationTokenSource();
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));

        await CreateSut().Handle(_command, cancellation.Token);

        await _inner.Received(1).Handle(_command, cancellation.Token);
    }

    [Fact]
    public async Task Handle_CommandWithoutResponse_LogsCommandTypeName()
    {
        var inner = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        inner.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));
        var logger = new FakeLogger<LoggingCommandHandlerDecorator<SampleCommand, Unit>>();
        var sut = new LoggingCommandHandlerDecorator<SampleCommand, Unit>(inner, logger, _timeProvider);

        await sut.Handle(new SampleCommand(1), CancellationToken.None);

        logger.LatestRecord.GetStructuredStateValue("RequestName").Should().Be(nameof(SampleCommand));
    }

    [Fact]
    public async Task Handle_CalledTwice_LogsOneRecordPerCall()
    {
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(Guid.NewGuid()), Result.Failure<Guid>(CommonErrors.NotFound));
        var sut = CreateSut();

        await sut.Handle(_command, CancellationToken.None);
        await sut.Handle(_command, CancellationToken.None);

        _logger.Collector.GetSnapshot().Select(record => record.Id.Id).Should().Equal(101, 102);
    }

    [Fact]
    public async Task Handle_DebugDisabled_DoesNotWriteSuccessRecordButStillWritesFailureRecord()
    {
        var collector = new FakeLogCollector(Microsoft.Extensions.Options.Options.Create(new FakeLogCollectorOptions()));
        var logger = new FilteredLogger(new FakeLogger<LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid>>(collector), LogLevel.Information);
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(Guid.NewGuid()), Result.Failure<Guid>(CommonErrors.NotFound));
        var sut = new LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid>(_inner, logger, _timeProvider);

        await sut.Handle(_command, CancellationToken.None);
        await sut.Handle(_command, CancellationToken.None);

        // 운영 최소 수준 Information에서는 성공 로그가 빠지고 실패 Result만 남는다.
        collector.GetSnapshot().Select(record => record.Id.Id).Should().Equal(102);
    }

    // ---- 조합: 커밋 실패 · 검증 실패도 로깅 데코레이터가 한 줄로 남긴다(전체 순서 등록은 S02-T03) ----

    [Fact]
    public async Task Handle_WrappingTransactionDecoratorWhoseCommitFails_LogsCommitErrorAsInformation()
    {
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));
        var sut = new LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid>(
            new TransactionCommandHandlerDecorator<RegisterPersonCommand, Guid>(_inner, unitOfWork), _logger, _timeProvider);

        var result = await sut.Handle(_command, CancellationToken.None);

        result.Error.Should().BeSameAs(CommonErrors.ConcurrencyConflict);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Information);
        record.GetStructuredStateValue("ErrorCode").Should().Be("3001");
        record.GetStructuredStateValue("ErrorType").Should().Be("30");
        PersonalData.ShouldNotContain([record]);
    }

    [Fact]
    public async Task Handle_WrappingValidationDecoratorWhoseValidationFails_LogsCode1001WithoutFieldValues()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        IValidator<RegisterPersonCommand>[] validators = [new RegisterPersonCommandValidator()];
        var sut = new LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid>(
            new ValidationCommandHandlerDecorator<RegisterPersonCommand, Guid>(
                new TransactionCommandHandlerDecorator<RegisterPersonCommand, Guid>(_inner, unitOfWork), validators),
            _logger,
            _timeProvider);
        var invalid = new RegisterPersonCommand(PersonalData.Name, "hong.gildong-at-example.com");

        var result = await sut.Handle(invalid, CancellationToken.None);

        result.Error.Should().BeOfType<ValidationError>();
        await _inner.DidNotReceive().Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(102);
        record.GetStructuredStateValue("ErrorCode").Should().Be("1001");
        PersonalData.ShouldNotContain([record], invalid.Email, SampleErrors.EmailInvalid.Message);
    }

    private LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid> CreateSut() => new(_inner, _logger, _timeProvider);

    // 최소 수준을 흉내 내는 로거. FakeLogger 자체는 모든 수준을 기록하므로 IsEnabled만 거른다.
    private sealed class FilteredLogger(ILogger<LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid>> inner, LogLevel minimumLevel)
        : ILogger<LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid>>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel && inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                inner.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}
