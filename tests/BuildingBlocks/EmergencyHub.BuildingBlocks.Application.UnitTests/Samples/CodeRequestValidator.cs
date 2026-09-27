using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>
/// 정의 여부(1002) 실패 뒤의 규칙이 실행되지 않는지(<c>RuleLevelCascadeMode = Stop</c>) 확인하는 Validator 예시입니다.
/// </summary>
internal sealed class CodeRequestValidator : RequestValidator<CodeRequest>
{
    public CodeRequestValidator()
    {
        RuleFor(request => request.Status)
            .MustBeDefinedEnum()
            .Must(_ => false).WithError(Error.Validation(21008, "두 번째 규칙"));
    }
}
