using EmergencyHub.BuildingBlocks.Application.Validation;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Validation;

// ADR-0018 중단 방식: 한 속성의 규칙 체인은 첫 실패에서 멈추고(RuleLevelCascadeMode = Stop), 여러 속성의 실패는 모두 모은다.
// 설정 위치는 공통 기반 RequestValidator<TRequest>로 정했다(전역 정적 설정은 테스트 · 호스트마다 달라질 수 있어 배제, S02-T02).
public sealed class RequestValidatorTests
{
    [Fact]
    public void Constructor_SetsRuleLevelCascadeModeToStopAndKeepsClassLevelContinue()
    {
        var validator = new RegisterPersonCommandValidator();

        validator.RuleLevelCascadeMode.Should().Be(CascadeMode.Stop);
        validator.ClassLevelCascadeMode.Should().Be(CascadeMode.Continue);
    }

    [Fact]
    public void Validate_EmptyName_ReportsNameRequiredOnly()
    {
        var validator = new RegisterPersonCommandValidator();

        var failures = validator.Validate(new RegisterPersonCommand(string.Empty, PersonalData.Email)).Errors;

        failures.Should().ContainSingle().Which.CustomState.Should().BeSameAs(SampleErrors.NameRequired);
    }

    [Fact]
    public void Validate_BothRulesOfSamePropertyWouldFail_StopsAtFirst()
    {
        var validator = new AlwaysFailingTwiceValidator();

        var failures = validator.Validate(new SampleCommand(1)).Errors;

        failures.Should().ContainSingle().Which.ErrorMessage.Should().Be("첫째");
    }

    [Fact]
    public void Validate_DifferentPropertiesFail_CollectsAllFailures()
    {
        var validator = new RegisterPersonCommandValidator();

        var failures = validator.Validate(new RegisterPersonCommand(string.Empty, "not-an-email")).Errors;

        failures.Select(failure => failure.PropertyName).Should().Equal("Name", "Email");
    }

    [Fact]
    public void Validate_ValidRequest_HasNoFailures()
    {
        new RegisterPersonCommandValidator().Validate(new RegisterPersonCommand(PersonalData.Name, PersonalData.Email)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MaximumLengthBoundary_AllowsMaxAndRejectsMaxPlusOne()
    {
        var validator = new RegisterPersonCommandValidator();

        validator.Validate(new RegisterPersonCommand(new string('가', 50), PersonalData.Email)).IsValid.Should().BeTrue();
        validator.Validate(new RegisterPersonCommand(new string('가', 51), PersonalData.Email)).Errors
            .Should().ContainSingle().Which.CustomState.Should().BeSameAs(SampleErrors.NameTooLong);
    }

    private sealed class AlwaysFailingTwiceValidator : RequestValidator<SampleCommand>
    {
        public AlwaysFailingTwiceValidator()
        {
            RuleFor(command => command.Value)
                .Must(_ => false).WithMessage("첫째")
                .Must(_ => false).WithMessage("둘째");
        }
    }
}
