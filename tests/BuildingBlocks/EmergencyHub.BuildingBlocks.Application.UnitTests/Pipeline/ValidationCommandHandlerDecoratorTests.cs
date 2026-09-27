using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using FluentValidation;
using FluentValidation.Results;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Pipeline;

// ADR-0015 · 0018 검증 데코레이터: Validator를 모두 실행해 실패가 있으면 inner(트랜잭션 · Handler)를 부르지 않고 ValidationError를 돌려준다.
public sealed class ValidationCommandHandlerDecoratorTests
{
    private readonly ICommandHandler<RegisterPersonCommand, Guid> _inner = Substitute.For<ICommandHandler<RegisterPersonCommand, Guid>>();

    public ValidationCommandHandlerDecoratorTests()
    {
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
    }

    public static TheoryData<string?> BlankMessages() => new(null, string.Empty, "   ");

    // ---- 성공 ----

    [Fact]
    public async Task Handle_NoValidators_CallsInnerAndReturnsSameResult()
    {
        var innerResult = Result.Success(Guid.NewGuid());
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(innerResult);
        var command = new RegisterPersonCommand(PersonalData.Name, PersonalData.Email);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.Should().BeSameAs(innerResult);
        await _inner.Received(1).Handle(Arg.Is<RegisterPersonCommand>(received => ReferenceEquals(received, command)), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_AllValidatorsPass_CallsInnerOnceAndReturnsSameResult()
    {
        var innerResult = Result.Success(Guid.NewGuid());
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(innerResult);
        var command = new RegisterPersonCommand(PersonalData.Name, PersonalData.Email);

        var result = await CreateSut(new RegisterPersonCommandValidator(), new InlineValidator<RegisterPersonCommand>()).Handle(command, CancellationToken.None);

        result.Should().BeSameAs(innerResult);
        await _inner.Received(1).Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ValidationPassesAndInnerFails_ReturnsSameInnerFailure()
    {
        var innerError = Error.Conflict(23001, "이미 등록된 이메일입니다.");
        _inner.Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<Guid>(innerError));

        var result = await CreateSut(new RegisterPersonCommandValidator())
            .Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        result.Error.Should().BeSameAs(innerError);
    }

    // ---- 실패: 검증 실패면 inner 미호출 ----

    [Fact]
    public async Task Handle_ValidationFails_DoesNotCallInnerAndReturnsValidationErrorWithFieldCodes()
    {
        var result = await CreateSut(new RegisterPersonCommandValidator())
            .Handle(new RegisterPersonCommand(string.Empty, "not-an-email"), CancellationToken.None);

        await _inner.DidNotReceive().Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
        result.IsFailure.Should().BeTrue();
        var error = result.Error.Should().BeOfType<ValidationError>().Subject;
        error.Code.Should().Be(1001);
        error.Type.Should().Be(ErrorType.Validation);
        error.Errors.Select(field => (field.PropertyName, field.Code)).Should().Equal(
            ("Name", SampleErrors.NameRequired.Code),
            ("Email", SampleErrors.EmailInvalid.Code));
        error.Errors[0].Message.Should().Be(SampleErrors.NameRequired.Message);
    }

    [Fact]
    public async Task Handle_SecondValidatorFails_RunsAllValidatorsAndCollectsFailuresInValidatorOrder()
    {
        var first = new InlineValidator<RegisterPersonCommand>();
        first.RuleFor(command => command.Email).Must(_ => false).WithState(_ => SampleErrors.EmailInvalid);
        var second = new InlineValidator<RegisterPersonCommand>();
        second.RuleFor(command => command.Name).Must(_ => false).WithState(_ => SampleErrors.NameTooLong);

        var result = await CreateSut(first, second).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        await _inner.DidNotReceive().Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
        var error = result.Error.Should().BeOfType<ValidationError>().Subject;
        error.Errors.Select(field => field.Code).Should().Equal(SampleErrors.EmailInvalid.Code, SampleErrors.NameTooLong.Code);
    }

    [Fact]
    public async Task Handle_OnlyFirstValidatorFails_StillRunsSecondValidator()
    {
        var first = Substitute.For<IValidator<RegisterPersonCommand>>();
        first.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "실패") { CustomState = SampleErrors.NameRequired }]));
        var second = Substitute.For<IValidator<RegisterPersonCommand>>();
        second.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());

        var result = await CreateSut(first, second).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await second.Received(1).ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
        await _inner.DidNotReceive().Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
    }

    // ---- BL-053: CustomState에 Error가 없는 실패 ----

    [Fact]
    public async Task Handle_FailureWithoutCustomState_WrapsAsCode1001WithFailureMessage()
    {
        var validator = new InlineValidator<RegisterPersonCommand>();
        validator.RuleFor(command => command.Name).NotEmpty().WithMessage("이름을 입력하세요.");

        var result = await CreateSut(validator).Handle(new RegisterPersonCommand(string.Empty, PersonalData.Email), CancellationToken.None);

        var field = result.Error.Should().BeOfType<ValidationError>().Subject.Errors.Should().ContainSingle().Subject;
        field.PropertyName.Should().Be("Name");
        field.Code.Should().Be(CommonErrors.ValidationFailed.Code);
        field.Message.Should().Be("이름을 입력하세요.");
    }

    [Fact]
    public async Task Handle_CustomStateIsNotError_WrapsAsCode1001()
    {
        var validator = new InlineValidator<RegisterPersonCommand>();
        validator.RuleFor(command => command.Name).Must(_ => false).WithState(_ => "문자열 상태").WithMessage("이름 오류");

        var result = await CreateSut(validator).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        var field = result.Error.Should().BeOfType<ValidationError>().Subject.Errors.Should().ContainSingle().Subject;
        field.Code.Should().Be(1001);
        field.Message.Should().Be("이름 오류");
    }

    [Theory]
    [MemberData(nameof(BlankMessages))]
    public async Task Handle_FailureWithoutCustomStateAndBlankMessage_UsesCommonValidationFailedMessage(string? message)
    {
        var validator = Substitute.For<IValidator<RegisterPersonCommand>>();
        validator.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", message)]));

        var result = await CreateSut(validator).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        var field = result.Error.Should().BeOfType<ValidationError>().Subject.Errors.Should().ContainSingle().Subject;
        field.Code.Should().Be(1001);
        field.Message.Should().Be(CommonErrors.ValidationFailed.Message);
    }

    [Fact]
    public async Task Handle_CustomStateIsValidationTypeError_UsesItsCodeAndMessage()
    {
        var validator = Substitute.For<IValidator<RegisterPersonCommand>>();
        validator.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Status", "무시되는 메시지") { CustomState = CommonErrors.InvalidCode }]));

        var result = await CreateSut(validator).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        var field = result.Error.Should().BeOfType<ValidationError>().Subject.Errors.Should().ContainSingle().Subject;
        field.Code.Should().Be(1002);
        field.Message.Should().Be(CommonErrors.InvalidCode.Message);
    }

    [Fact]
    public async Task Handle_CustomStateIsNonValidationError_ThrowsBecauseRuleIsMisconfigured()
    {
        var validator = new InlineValidator<RegisterPersonCommand>();
        validator.RuleFor(command => command.Name).Must(_ => false).WithState(_ => CommonErrors.NotFound);

        var act = () => CreateSut(validator).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        // 필드별 상세는 검증 실패 유형만 담는다(FieldError). 다른 유형을 실은 규칙은 프로그래밍 오류라 예외로 드러낸다.
        await act.Should().ThrowAsync<ArgumentException>();
        await _inner.DidNotReceive().Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Handle_FailureWithNullPropertyName_UsesEmptyPropertyNameForObjectLevelRule()
    {
        var validator = Substitute.For<IValidator<RegisterPersonCommand>>();
        validator.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure(null, "객체 수준 오류")]));

        var result = await CreateSut(validator).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        result.Error.Should().BeOfType<ValidationError>().Subject.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CollectionItemFailure_KeepsIndexedPropertyPath()
    {
        var validator = Substitute.For<IValidator<RegisterPersonCommand>>();
        validator.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Items[0].Name", "항목 이름 오류") { CustomState = SampleErrors.NameRequired }]));

        var result = await CreateSut(validator).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        result.Error.Should().BeOfType<ValidationError>().Subject.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Items[0].Name");
    }

    [Fact]
    public async Task Handle_SamePropertyFailsTwiceAcrossValidators_KeepsBothFieldErrors()
    {
        var first = new InlineValidator<RegisterPersonCommand>();
        first.RuleFor(command => command.Name).Must(_ => false).WithState(_ => SampleErrors.NameRequired);
        var second = new InlineValidator<RegisterPersonCommand>();
        second.RuleFor(command => command.Name).Must(_ => false).WithState(_ => SampleErrors.NameTooLong);

        var result = await CreateSut(first, second).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        result.Error.Should().BeOfType<ValidationError>().Subject.Errors.Select(field => field.PropertyName).Should().Equal("Name", "Name");
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesSameTokenToEveryValidatorAndInner()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var first = Substitute.For<IValidator<RegisterPersonCommand>>();
        first.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var second = Substitute.For<IValidator<RegisterPersonCommand>>();
        second.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var command = new RegisterPersonCommand(PersonalData.Name, PersonalData.Email);

        await CreateSut(first, second).Handle(command, token);

        await first.Received(1).ValidateAsync(command, token);
        await second.Received(1).ValidateAsync(command, token);
        await _inner.Received(1).Handle(command, token);
    }

    [Fact]
    public async Task Handle_ValidatorThrows_PropagatesSameExceptionWithoutCallingInner()
    {
        var exception = new InvalidOperationException("Validator 오류");
        var validator = Substitute.For<IValidator<RegisterPersonCommand>>();
        validator.ValidateAsync(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<ValidationResult>(exception));

        var act = () => CreateSut(validator).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), CancellationToken.None);

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        await _inner.DidNotReceive().Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidatorCanceled_PropagatesOperationCanceledWithoutCallingInner()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var validator = new InlineValidator<RegisterPersonCommand>();
        validator.RuleFor(command => command.Name).MustAsync(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return true;
        });

        var act = () => CreateSut(validator).Handle(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await _inner.DidNotReceive().Handle(Arg.Any<RegisterPersonCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommandWithoutResponse_ReturnsUnitFailureOnValidationError()
    {
        var inner = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        var validator = new InlineValidator<SampleCommand>();
        validator.RuleFor(command => command.Value).GreaterThan(0).WithState(_ => CommonErrors.InvalidCode);
        var sut = new ValidationCommandHandlerDecorator<SampleCommand, Unit>(inner, [validator]);

        Result<Unit> result = await sut.Handle(new SampleCommand(0), CancellationToken.None);

        result.Error.Should().BeOfType<ValidationError>().Which.Errors.Should().ContainSingle().Which.Code.Should().Be(1002);
        await inner.DidNotReceive().Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>());
    }

    private ValidationCommandHandlerDecorator<RegisterPersonCommand, Guid> CreateSut(params IValidator<RegisterPersonCommand>[] validators) =>
        new(_inner, validators);
}
