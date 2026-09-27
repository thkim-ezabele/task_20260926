using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Acceptance;

// PRD-001 FR-05 중 S02-T02 범위(데코레이터 동작)의 인수 시나리오(tester 추가분).
// 인수 조건 "검증 실패 시 Handler가 호출되지 않으며(실패도 로그에 남음), 실패 Result면 커밋하지 않음"을
// ADR-0015 순서(로깅 → 검증 → 트랜잭션 → Handler)로 직접 조립한 체인에서 끝까지 확인한다.
// DI 등록으로 이 순서가 실제로 만들어지는지는 S02-T03이 검증한다(여기서는 수동 조립).
// developer 단위 테스트가 데코레이터 하나씩 확인한 항목은 반복하지 않고, 체인 전체에서만 드러나는 것(호출 순서,
// 단계별 미호출, 한 요청당 로그 1건, 커밋 시간이 포함된 경과 시간, 토큰 전파, 인스턴스 동시 사용)만 둔다.
[Trait("FR", "PRD-001/FR-05")]
public sealed class DecoratorChainAcceptanceTests
{
    private readonly IValidator<RegisterPersonCommand> _validator = Substitute.For<IValidator<RegisterPersonCommand>>();
    private readonly ICommandHandler<RegisterPersonCommand, Guid> _handler = Substitute.For<ICommandHandler<RegisterPersonCommand, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeLogger<LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid>> _commandLogger = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly RegisterPersonCommand _command = new(PersonalData.Name, PersonalData.Email);

    public DecoratorChainAcceptanceTests()
    {
        _validator.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        _handler.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    // ---- 성공 ----

    [Fact]
    public async Task CommandChain_AllStagesSucceed_RunsValidatorHandlerCommitInOrderAndLogsOneDebugRecord()
    {
        var createdId = Guid.NewGuid();
        _handler.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(createdId));

        var result = await CreateCommandChain().Handle(_command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(createdId);
        Received.InOrder(() =>
        {
            _validator.ValidateAsync(_command, CancellationToken.None);
            _handler.Handle(_command, CancellationToken.None);
            _unitOfWork.CommitAsync(CancellationToken.None);
        });
        var record = _commandLogger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(101);
        record.Level.Should().Be(LogLevel.Debug);
        PersonalData.ShouldNotContain([record], createdId.ToString());
    }

    [Fact]
    public async Task CommandChain_CommitTakesTime_LoggedElapsedIncludesHandlerAndCommitDuration()
    {
        _handler.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(40));
            return Result.Success(Guid.NewGuid());
        });
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(60));
            return Result.Success();
        });

        await CreateCommandChain().Handle(_command, CancellationToken.None);

        // 로깅이 가장 바깥이므로 커밋(UoW 안의 SaveChanges · 커밋) 시간까지 한 요청의 경과 시간에 들어간다.
        _commandLogger.LatestRecord.GetStructuredStateValue("ElapsedMilliseconds").Should().Be("100");
    }

    // ---- 실패: 검증 실패 → Handler · 커밋 미호출, 실패 로그 ----

    [Fact]
    public async Task CommandChain_ValidationFails_DoesNotCallHandlerOrCommitAndLogsOneInformationRecordWithCode1001()
    {
        IValidator<RegisterPersonCommand>[] validators = [new RegisterPersonCommandValidator()];
        var invalid = new RegisterPersonCommand(string.Empty, "hong.gildong-at-example.com");

        var result = await CreateCommandChain(validators).Handle(invalid, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        var error = result.Error.Should().BeOfType<ValidationError>().Subject;
        error.Code.Should().Be(CommonErrors.ValidationFailed.Code);
        error.Errors.Select(field => field.Code).Should().Equal(SampleErrors.NameRequired.Code, SampleErrors.EmailInvalid.Code);
        await _handler.DidNotReceive().Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());

        var record = _commandLogger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(102);
        record.Level.Should().Be(LogLevel.Information);
        record.GetStructuredStateValue("ErrorCode").Should().Be("1001");
        record.GetStructuredStateValue("ErrorType").Should().Be(((short)ErrorType.Validation).ToString(System.Globalization.CultureInfo.InvariantCulture));
        PersonalData.ShouldNotContain([record], invalid.Email, SampleErrors.NameRequired.Message, SampleErrors.EmailInvalid.Message);
    }

    // ---- 실패: Handler 실패 Result → 커밋 미호출 ----

    [Fact]
    public async Task CommandChain_HandlerReturnsFailure_DoesNotCommitAndLogsHandlerErrorCode()
    {
        var handlerError = Error.Conflict(23001, "이미 등록된 이메일입니다.");
        _handler.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<Guid>(handlerError));

        var result = await CreateCommandChain().Handle(_command, CancellationToken.None);

        result.Error.Should().BeSameAs(handlerError);
        await _validator.Received(1).ValidateAsync(_command, Arg.Any<CancellationToken>());
        await _handler.Received(1).Handle(_command, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        var record = _commandLogger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(102);
        record.GetStructuredStateValue("ErrorCode").Should().Be("23001");
    }

    // ---- 실패: 커밋 실패 Result → 커밋의 Error 그대로 전달 · 기록 ----

    [Fact]
    public async Task CommandChain_CommitReturnsFailure_ReturnsSameCommitErrorAndLogsItOnce()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await CreateCommandChain().Handle(_command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(CommonErrors.ConcurrencyConflict);
        await _handler.Received(1).Handle(_command, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        var record = _commandLogger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(102);
        record.GetStructuredStateValue("ErrorCode").Should().Be(CommonErrors.ConcurrencyConflict.Code.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // ---- 실패: 예외는 체인 어디서도 잡거나 기록하지 않는다 ----

    [Fact]
    public async Task CommandChain_HandlerThrows_PropagatesSameExceptionWithoutCommitOrLog()
    {
        var exception = new InvalidOperationException("예상하지 못한 오류");
        _handler.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Result<Guid>>(exception));

        var act = () => CreateCommandChain().Handle(_command, CancellationToken.None);

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        _commandLogger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task CommandChain_CommitThrows_PropagatesSameExceptionWithHandlerCalledOnceAndNoLog()
    {
        var exception = new TimeoutException("재시도 한도 초과 가정");
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<Result>(exception));

        var act = () => CreateCommandChain().Handle(_command, CancellationToken.None);

        (await act.Should().ThrowExactlyAsync<TimeoutException>()).Which.Should().BeSameAs(exception);
        await _handler.Received(1).Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
        _commandLogger.Collector.Count.Should().Be(0);
    }

    // ---- 엣지: 취소 토큰이 체인 끝까지 같은 값으로 전달된다 ----

    [Fact]
    public async Task CommandChain_WithCancellationToken_PassesSameTokenToValidatorHandlerAndCommit()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;

        await CreateCommandChain().Handle(_command, token);

        await _validator.Received(1).ValidateAsync(_command, token);
        await _handler.Received(1).Handle(_command, token);
        await _unitOfWork.Received(1).CommitAsync(token);
    }

    // ---- 엣지: 데코레이터는 상태가 없어 한 체인 인스턴스를 동시에 써도 요청마다 커밋 · 로그가 1건씩이다 ----

    [Fact]
    public async Task CommandChain_UsedConcurrently_CommitsAndLogsExactlyOncePerRequest()
    {
        const int RequestCount = 32;
        var chain = CreateCommandChain();

        await Task.WhenAll(Enumerable.Range(0, RequestCount).Select(_ => Task.Run(() => chain.Handle(_command, CancellationToken.None))));

        await _handler.Received(RequestCount).Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(RequestCount).CommitAsync(Arg.Any<CancellationToken>());
        _commandLogger.Collector.GetSnapshot().Should().HaveCount(RequestCount).And.OnlyContain(record => record.Id.Id == 101);
    }

    // ---- Query: 로깅 → 검증 → Handler(트랜잭션 없음) ----

    [Fact]
    public async Task QueryChain_ValidationFails_DoesNotCallHandlerAndLogsOneInformationRecordWithEventId104()
    {
        var handler = Substitute.For<IQueryHandler<FindPersonQuery, SampleResponse>>();
        var validator = new InlineValidator<FindPersonQuery>();
        validator.RuleFor(query => query.Email).EmailAddress();
        var logger = new FakeLogger<LoggingQueryHandlerDecorator<FindPersonQuery, SampleResponse>>();
        var chain = CreateQueryChain(handler, validator, logger);
        var invalid = new FindPersonQuery("hong.gildong-at-example.com");

        var result = await chain.Handle(invalid, CancellationToken.None);

        // WithError를 붙이지 않은 규칙의 실패도 1001로 감싸져 전달된다(BL-053).
        var error = result.Error.Should().BeOfType<ValidationError>().Subject;
        error.Errors.Should().ContainSingle().Which.Code.Should().Be(CommonErrors.ValidationFailed.Code);
        await handler.DidNotReceive().Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>());
        var record = logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(104);
        record.Level.Should().Be(LogLevel.Information);
        record.GetStructuredStateValue("ErrorCode").Should().Be("1001");
        PersonalData.ShouldNotContain([record], invalid.Email);
    }

    [Fact]
    public async Task QueryChain_AllStagesSucceed_ReturnsHandlerResultAndLogsOneDebugRecordWithEventId103()
    {
        var response = new SampleResponse(7, PersonalData.Name);
        var handler = Substitute.For<IQueryHandler<FindPersonQuery, SampleResponse>>();
        handler.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(response));
        var validator = new InlineValidator<FindPersonQuery>();
        validator.RuleFor(query => query.Email).EmailAddress();
        var logger = new FakeLogger<LoggingQueryHandlerDecorator<FindPersonQuery, SampleResponse>>();
        var query = new FindPersonQuery(PersonalData.Email);

        var result = await CreateQueryChain(handler, validator, logger).Handle(query, CancellationToken.None);

        result.Value.Should().BeSameAs(response);
        await handler.Received(1).Handle(query, CancellationToken.None);
        var record = logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(103);
        record.Level.Should().Be(LogLevel.Debug);
        PersonalData.ShouldNotContain([record]);
    }

    // ADR-0015 순서: 로깅(바깥) → 검증 → 트랜잭션 → Handler(안쪽). DI 조립은 S02-T03.
    private LoggingCommandHandlerDecorator<RegisterPersonCommand, Guid> CreateCommandChain(IEnumerable<IValidator<RegisterPersonCommand>>? validators = null) =>
        new(
            new ValidationCommandHandlerDecorator<RegisterPersonCommand, Guid>(
                new TransactionCommandHandlerDecorator<RegisterPersonCommand, Guid>(_handler, _unitOfWork),
                validators ?? [_validator]),
            _commandLogger,
            _timeProvider);

    // Query 체인에는 트랜잭션 데코레이터가 없다(트랜잭션 데코레이터는 ICommandHandler만 구현).
    private LoggingQueryHandlerDecorator<FindPersonQuery, SampleResponse> CreateQueryChain(
        IQueryHandler<FindPersonQuery, SampleResponse> handler,
        IValidator<FindPersonQuery> validator,
        FakeLogger<LoggingQueryHandlerDecorator<FindPersonQuery, SampleResponse>> logger) =>
        new(new ValidationQueryHandlerDecorator<FindPersonQuery, SampleResponse>(handler, [validator]), logger, _timeProvider);
}
