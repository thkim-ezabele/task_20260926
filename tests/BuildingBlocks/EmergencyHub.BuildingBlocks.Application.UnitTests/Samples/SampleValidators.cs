using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>테스트용 필드별 오류 정의(검증 실패 유형, 서비스 자리 2 = Employee 가정).</summary>
public static class SampleErrors
{
    public static readonly Error NameRequired = Error.Validation(21001, "이름은 필수입니다.");

    public static readonly Error NameTooLong = Error.Validation(21002, "이름은 50자 이하여야 합니다.");

    public static readonly Error EmailInvalid = Error.Validation(21003, "이메일 형식이 올바르지 않습니다.");
}

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
