using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Validation;

// ADR-0018 WithError(Error): 규칙에 정수 코드를 붙인다. CustomState에 Error, 메시지는 Error.Message.
public sealed class ValidationRuleExtensionsTests
{
    public static TheoryData<Error> NonValidationErrors() =>
        new(
            CommonErrors.NotFound,
            CommonErrors.ConcurrencyConflict,
            Error.BusinessRule(24001, "허용되지 않은 상태 전이입니다."),
            CommonErrors.Unauthenticated,
            CommonErrors.Forbidden,
            CommonErrors.Unexpected,
            CommonErrors.ExternalServiceFailed,
            CommonErrors.TemporarilyUnavailable);

    // ---- 성공 ----

    [Fact]
    public void WithError_RuleFails_PutsSameErrorInCustomStateAndUsesItsMessage()
    {
        var validator = new InlineValidator<RegisterPersonCommand>();
        validator.RuleFor(command => command.Name).NotEmpty().WithError(SampleErrors.NameRequired);

        var failure = validator.Validate(new RegisterPersonCommand(string.Empty, PersonalData.Email)).Errors.Should().ContainSingle().Subject;

        failure.CustomState.Should().BeSameAs(SampleErrors.NameRequired);
        failure.ErrorMessage.Should().Be(SampleErrors.NameRequired.Message);
        failure.PropertyName.Should().Be("Name");
    }

    [Fact]
    public void WithError_RulePasses_ProducesNoFailure()
    {
        var validator = new InlineValidator<RegisterPersonCommand>();
        validator.RuleFor(command => command.Name).NotEmpty().WithError(SampleErrors.NameRequired);

        validator.Validate(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void WithError_EachChainedRule_KeepsItsOwnError()
    {
        var validator = new InlineValidator<RegisterPersonCommand>();
        validator.RuleFor(command => command.Name)
            .NotEmpty().WithError(SampleErrors.NameRequired)
            .MaximumLength(2).WithError(SampleErrors.NameTooLong);

        var failure = validator.Validate(new RegisterPersonCommand("홍길동전", PersonalData.Email)).Errors.Should().ContainSingle().Subject;

        failure.CustomState.Should().BeSameAs(SampleErrors.NameTooLong);
    }

    [Fact]
    public void WithError_CommonCodes_AreAccepted()
    {
        var validator = new InlineValidator<SampleCommand>();
        validator.RuleFor(command => command.Value).GreaterThan(0).WithError(CommonErrors.InvalidCode);

        var failure = validator.Validate(new SampleCommand(0)).Errors.Should().ContainSingle().Subject;

        failure.CustomState.Should().BeSameAs(CommonErrors.InvalidCode);
    }

    [Fact]
    public void WithError_ValidationErrorInstance_IsAcceptedBecauseTypeIsValidation()
    {
        var validationError = ValidationError.Create([FieldError.Create("Name", SampleErrors.NameRequired)]);
        var validator = new InlineValidator<RegisterPersonCommand>();

        var act = () => validator.RuleFor(command => command.Name).NotEmpty().WithError(validationError);

        act.Should().NotThrow();
    }

    // ---- 실패 ----

    [Fact]
    public void WithError_NullError_ThrowsArgumentNullException()
    {
        var validator = new InlineValidator<RegisterPersonCommand>();

        var act = () => validator.RuleFor(command => command.Name).NotEmpty().WithError(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    [Fact]
    public void WithError_NullRule_ThrowsArgumentNullException()
    {
        IRuleBuilderOptions<RegisterPersonCommand, string> rule = null!;

        var act = () => rule.WithError(SampleErrors.NameRequired);

        act.Should().Throw<ArgumentNullException>().WithParameterName("rule");
    }

    [Theory]
    [MemberData(nameof(NonValidationErrors))]
    public void WithError_NonValidationTypeError_ThrowsArgumentException(Error error)
    {
        var validator = new InlineValidator<RegisterPersonCommand>();

        // 필드별 상세(FieldError)는 검증 실패 유형만 담으므로 규칙 정의 시점에 막는다.
        var act = () => validator.RuleFor(command => command.Name).NotEmpty().WithError(error);

        act.Should().Throw<ArgumentException>().WithParameterName(nameof(error));
    }

    // ---- 엣지 ----

    [Fact]
    public void WithError_MessageWithUnicode_IsKeptAsIs()
    {
        var error = Error.Validation(21009, "이름에 😀 이모지는 쓸 수 없습니다.");
        var validator = new InlineValidator<RegisterPersonCommand>();
        validator.RuleFor(command => command.Name).Must(_ => false).WithError(error);

        var failure = validator.Validate(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email)).Errors.Should().ContainSingle().Subject;

        failure.ErrorMessage.Should().Be(error.Message);
    }
}
