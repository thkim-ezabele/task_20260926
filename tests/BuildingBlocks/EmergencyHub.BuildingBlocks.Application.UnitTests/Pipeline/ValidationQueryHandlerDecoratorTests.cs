using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using FluentValidation;
using FluentValidation.Results;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Pipeline;

// Query 검증 데코레이터. 실패 변환 규칙은 Command와 같은 내부 도우미를 쓰므로 Query 경로에서 호출 여부 · 결과 전달을 확인한다.
public sealed class ValidationQueryHandlerDecoratorTests
{
    private readonly IQueryHandler<FindPersonQuery, SampleResponse> _inner = Substitute.For<IQueryHandler<FindPersonQuery, SampleResponse>>();

    // ---- 성공 ----

    [Fact]
    public async Task Handle_NoValidators_CallsInnerAndReturnsSameResult()
    {
        var innerResult = Result.Success(new SampleResponse(1, PersonalData.Name));
        _inner.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(innerResult);
        var query = new FindPersonQuery(PersonalData.Email);

        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.Should().BeSameAs(innerResult);
        await _inner.Received(1).Handle(Arg.Is<FindPersonQuery>(received => ReferenceEquals(received, query)), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ValidatorPasses_CallsInnerAndReturnsSameResult()
    {
        var innerResult = Result.Failure<SampleResponse>(CommonErrors.NotFound);
        _inner.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(innerResult);
        var validator = new InlineValidator<FindPersonQuery>();
        validator.RuleFor(query => query.Email).NotEmpty();

        var result = await CreateSut(validator).Handle(new FindPersonQuery(PersonalData.Email), CancellationToken.None);

        result.Should().BeSameAs(innerResult);
    }

    // ---- 실패 ----

    [Fact]
    public async Task Handle_ValidationFails_DoesNotCallInnerAndReturnsValidationError()
    {
        var validator = new InlineValidator<FindPersonQuery>();
        validator.RuleFor(query => query.Email).NotEmpty().WithState(_ => SampleErrors.EmailInvalid);

        var result = await CreateSut(validator).Handle(new FindPersonQuery(string.Empty), CancellationToken.None);

        await _inner.DidNotReceive().Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>());
        var error = result.Error.Should().BeOfType<ValidationError>().Subject;
        error.Code.Should().Be(1001);
        error.Errors.Select(field => field.Code).Should().Equal(SampleErrors.EmailInvalid.Code);
    }

    [Fact]
    public async Task Handle_FailureWithoutCustomState_WrapsAsCode1001()
    {
        var validator = Substitute.For<IValidator<FindPersonQuery>>();
        validator.ValidateAsync(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Email", "이메일 오류")]));

        var result = await CreateSut(validator).Handle(new FindPersonQuery(PersonalData.Email), CancellationToken.None);

        var field = result.Error.Should().BeOfType<ValidationError>().Subject.Errors.Should().ContainSingle().Subject;
        field.Code.Should().Be(1001);
        field.Message.Should().Be("이메일 오류");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Handle_WithCancellationToken_PassesSameTokenToValidatorAndInner()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _inner.Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new SampleResponse(1, "샘플")));
        var validator = Substitute.For<IValidator<FindPersonQuery>>();
        validator.ValidateAsync(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var query = new FindPersonQuery(PersonalData.Email);

        await CreateSut(validator).Handle(query, token);

        await validator.Received(1).ValidateAsync(query, token);
        await _inner.Received(1).Handle(query, token);
    }

    [Fact]
    public async Task Handle_ValidatorThrows_PropagatesSameExceptionWithoutCallingInner()
    {
        var exception = new InvalidOperationException("Validator 오류");
        var validator = Substitute.For<IValidator<FindPersonQuery>>();
        validator.ValidateAsync(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<ValidationResult>(exception));

        var act = () => CreateSut(validator).Handle(new FindPersonQuery(PersonalData.Email), CancellationToken.None);

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        await _inner.DidNotReceive().Handle(Arg.Any<FindPersonQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Decorator_ImplementsOnlyQueryHandler()
    {
        typeof(ValidationQueryHandlerDecorator<,>).GetInterfaces().Should().ContainSingle()
            .Which.GetGenericTypeDefinition().Should().Be(typeof(IQueryHandler<,>));
    }

    private ValidationQueryHandlerDecorator<FindPersonQuery, SampleResponse> CreateSut(params IValidator<FindPersonQuery>[] validators) =>
        new(_inner, validators);
}
