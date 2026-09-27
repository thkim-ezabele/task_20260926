using EmergencyHub.BuildingBlocks.Application.Validation;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary><see cref="GetSampleQuery"/> Validator.</summary>
public sealed class GetSampleQueryValidator : RequestValidator<GetSampleQuery>
{
    /// <summary>규칙을 정의합니다.</summary>
    /// <param name="probe">실행 기록 탐침.</param>
    public GetSampleQueryValidator(IPipelineProbe probe)
    {
        RuleFor(query => query.Value)
            .Must(value =>
            {
                probe.Validating(value);
                return value >= 0;
            })
            .WithError(SampleErrors.ValueNegative);
    }
}
