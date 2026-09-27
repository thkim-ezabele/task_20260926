using EmergencyHub.BuildingBlocks.Application.Validation;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary><see cref="ChannelFilterQuery"/> Validator. 두 속성 모두 정의된 코드값 규칙(1002)만 둡니다.</summary>
public sealed class ChannelFilterQueryValidator : RequestValidator<ChannelFilterQuery>
{
    /// <summary>규칙을 정의합니다.</summary>
    public ChannelFilterQueryValidator()
    {
        RuleFor(query => query.Status).MustBeDefinedEnum();
        RuleFor(query => query.Channels).MustBeDefinedEnum();
    }
}
