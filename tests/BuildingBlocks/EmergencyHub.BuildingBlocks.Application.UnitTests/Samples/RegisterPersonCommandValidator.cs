using EmergencyHub.BuildingBlocks.Application.Validation;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>공통 기반 <see cref="RequestValidator{TRequest}"/>를 쓴 Validator 예시.</summary>
public sealed class RegisterPersonCommandValidator : RequestValidator<RegisterPersonCommand>
{
    public RegisterPersonCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithError(SampleErrors.NameRequired)
            .MaximumLength(50).WithError(SampleErrors.NameTooLong);

        RuleFor(command => command.Email)
            .EmailAddress().WithError(SampleErrors.EmailInvalid);
    }
}
