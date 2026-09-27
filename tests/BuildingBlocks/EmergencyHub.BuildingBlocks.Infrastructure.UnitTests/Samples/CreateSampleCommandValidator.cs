using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Validation;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary><see cref="CreateSampleCommand"/>의 internal Validator(FluentValidation 검색이 internal 형식도 찾는지 확인).</summary>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 FluentValidation 검색(includeInternalTypes)으로 등록해 DI가 만든다(테스트 대상 동작).")]
internal sealed class CreateSampleCommandValidator : RequestValidator<CreateSampleCommand>
{
    public CreateSampleCommandValidator(IPipelineProbe probe)
    {
        RuleFor(command => command.Value)
            .Must(value =>
            {
                probe.Validating(value);
                return value >= 0;
            })
            .WithError(SampleErrors.ValueNegative);
    }
}
