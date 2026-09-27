using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Pipeline;

// ADR-0014 · 0015 트랜잭션 데코레이터: Handler를 한 번 실행하고, 성공 Result일 때만 IUnitOfWork.CommitAsync를 부른다.
// 데코레이터는 IUnitOfWork만 의존하므로 DbContext · 트랜잭션 · 실행 전략 · SaveChanges는 이 테스트에 나타나지 않는다(S02-T07).
public sealed class TransactionCommandHandlerDecoratorTests
{
    private static readonly Error HandlerError = Error.BusinessRule(24001, "허용되지 않은 상태 전이입니다.");

    private readonly ICommandHandler<CreateSampleCommand, Guid> _inner = Substitute.For<ICommandHandler<CreateSampleCommand, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public static TheoryData<Error> HandlerFailures() =>
        new(
            HandlerError,
            CommonErrors.NotFound,
            CommonErrors.ValidationFailed,
            ValidationError.Create([FieldError.Create("Name", CommonErrors.ValidationFailed)]));

    public static TheoryData<Error> CommitFailures() =>
        new(
            CommonErrors.ConcurrencyConflict,
            Error.Conflict(23001, "이미 등록된 이메일입니다."),
            CommonErrors.TemporarilyUnavailable);

    public static TheoryData<Exception> CommitExceptions() =>
        new(
        new Exception[]
        {
            new InvalidOperationException("check 제약 위반(23514 가정)"),
            new OperationCanceledException(),
            new TimeoutException("재시도 한도 초과 가정"),
        });

    // ---- 성공 ----

    [Fact]
    public async Task Handle_HandlerSucceedsAndCommitSucceeds_CommitsOnceAndReturnsSameHandlerResult()
    {
        var handlerResult = Result.Success(Guid.NewGuid());
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(handlerResult);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        var command = new CreateSampleCommand("샘플");

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.Should().BeSameAs(handlerResult);
        await _inner.Received(1).Handle(Arg.Is<CreateSampleCommand>(received => ReferenceEquals(received, command)), CancellationToken.None);
        await _unitOfWork.Received(1).CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_CommandWithoutResponse_CommitsAndReturnsUnitSuccess()
    {
        var inner = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        inner.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        var sut = new TransactionCommandHandlerDecorator<SampleCommand, Unit>(inner, _unitOfWork);

        var result = await sut.Handle(new SampleCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HandlerSucceeds_CommitsAfterHandlerCompletes()
    {
        var order = new List<string>();
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            order.Add("handler");
            return Result.Success(Guid.NewGuid());
        });
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            order.Add("commit");
            return Result.Success();
        });

        await CreateSut().Handle(new CreateSampleCommand("샘플"), CancellationToken.None);

        order.Should().Equal("handler", "commit");
    }

    // ---- 실패: Handler 실패 Result ----

    [Theory]
    [MemberData(nameof(HandlerFailures))]
    public async Task Handle_HandlerReturnsFailure_DoesNotCommitAndReturnsSameError(Error error)
    {
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<Guid>(error));

        var result = await CreateSut().Handle(new CreateSampleCommand("샘플"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---- 실패: CommitAsync 실패 Result ----

    [Theory]
    [MemberData(nameof(CommitFailures))]
    public async Task Handle_CommitReturnsFailure_ReturnsCommitErrorInsteadOfHandlerValue(Error commitError)
    {
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(commitError));

        var result = await CreateSut().Handle(new CreateSampleCommand("샘플"), CancellationToken.None);

        // 코드 · 형식을 바꾸거나 감싸지 않고 UoW의 Error 인스턴스를 그대로 전달한다(dba 조건 6).
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(commitError);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---- 실패: 예외는 잡지 않는다 ----

    [Fact]
    public async Task Handle_HandlerThrows_PropagatesSameExceptionWithoutCommit()
    {
        var exception = new InvalidOperationException("예상하지 못한 오류");
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns<Task<Result<Guid>>>(_ => throw exception);

        var act = () => CreateSut().Handle(new CreateSampleCommand("샘플"), CancellationToken.None);

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HandlerReturnsFaultedTask_PropagatesSameExceptionWithoutCommit()
    {
        var exception = new InvalidOperationException("비동기 오류");
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Result<Guid>>(exception));

        var act = () => CreateSut().Handle(new CreateSampleCommand("샘플"), CancellationToken.None);

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(CommitExceptions))]
    public async Task Handle_CommitThrows_PropagatesSameExceptionAndCallsHandlerOnce(Exception exception)
    {
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<Result>(exception));

        var act = () => CreateSut().Handle(new CreateSampleCommand("샘플"), CancellationToken.None);

        // 23514 · 재시도 한도 초과 같은 예상하지 못한 영속성 오류와 취소는 예외 그대로 올라간다(재시도 루프 없음, dba 조건 2 · 8).
        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(exception);
        await _inner.Received(1).Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---- 엣지: 취소 토큰 ----

    [Fact]
    public async Task Handle_WithCancellationToken_PassesSameTokenToHandlerAndCommit()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

        await CreateSut().Handle(new CreateSampleCommand("샘플"), token);

        await _inner.Received(1).Handle(Arg.Any<CreateSampleCommand>(), token);
        await _unitOfWork.Received(1).CommitAsync(token);
    }

    [Fact]
    public async Task Handle_TokenAlreadyCanceled_DelegatesCancellationToHandlerAndUnitOfWork()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromCanceled<Result>(cancellation.Token));

        var act = () => CreateSut().Handle(new CreateSampleCommand("샘플"), cancellation.Token);

        // 데코레이터는 토큰을 직접 검사하지 않고 그대로 넘긴다. 취소 판단 · 예외는 Handler / UoW 몫이다.
        await act.Should().ThrowAsync<OperationCanceledException>();
        await _unitOfWork.Received(1).CommitAsync(cancellation.Token);
    }

    // ---- 엣지: 같은 인스턴스 반복 사용 ----

    [Fact]
    public async Task Handle_CalledTwice_CommitsOncePerCall()
    {
        _inner.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        var sut = CreateSut();

        await sut.Handle(new CreateSampleCommand("첫째"), CancellationToken.None);
        await sut.Handle(new CreateSampleCommand("둘째"), CancellationToken.None);

        await _inner.Received(2).Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---- 구조: Command 전용, IUnitOfWork만 의존 ----

    [Fact]
    public void Decorator_ImplementsOnlyCommandHandlerAndDependsOnlyOnInnerAndUnitOfWork()
    {
        var type = typeof(TransactionCommandHandlerDecorator<,>);

        type.GetInterfaces().Should().ContainSingle()
            .Which.GetGenericTypeDefinition().Should().Be(typeof(ICommandHandler<,>));
        type.GetConstructors().Should().ContainSingle()
            .Which.GetParameters()
            .Select(parameter => parameter.ParameterType.IsGenericType
                ? parameter.ParameterType.GetGenericTypeDefinition()
                : parameter.ParameterType)
            .Should().Equal(typeof(ICommandHandler<,>), typeof(IUnitOfWork));
    }

    private TransactionCommandHandlerDecorator<CreateSampleCommand, Guid> CreateSut() => new(_inner, _unitOfWork);
}
