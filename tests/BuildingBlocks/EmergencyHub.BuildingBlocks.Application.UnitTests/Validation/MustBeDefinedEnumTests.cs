using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Validation;

// ADR-0018 "정의되지 않은 코드값": MustBeDefinedEnum()은 정의되지 않은 정수를 1002(Common.InvalidCode)로 거부한다(S02-T06).
// 일반 enum은 Enum.IsDefined + 0(예약 값) 거부, [Flags]는 정의된 비트의 조합(0 포함)만 허용한다.
public sealed class MustBeDefinedEnumTests
{
    private static readonly CodeRequest Valid = new(SampleStatus.Active, SampleChannels.Sms, WideSampleChannels.First);

    public static TheoryData<short> UndefinedStatuses() => new(2, 4, 99, -1, short.MinValue, short.MaxValue);

    public static TheoryData<int> DefinedChannelCombinations() => new(0, 1, 2, 3, 4, 5, 6, 7);

    public static TheoryData<int> UndefinedChannelBits() => new(8, 1 | 8, 1 << 30, int.MinValue, -1);

    // ---- 성공 ----

    [Theory]
    [InlineData(SampleStatus.Active)]
    [InlineData(SampleStatus.Retired)]
    public void MustBeDefinedEnum_DefinedNonZeroCode_Passes(SampleStatus status)
    {
        var result = CreateStatusValidator().Validate(Valid with { Status = status });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(DefinedChannelCombinations))]
    public void MustBeDefinedEnum_FlagsCombinationOfDefinedBits_Passes(int channels)
    {
        var result = CreateChannelsValidator().Validate(Valid with { Channels = (SampleChannels)channels });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MustBeDefinedEnum_FailingCode_ReportsInvalidCodeInCustomStateWithItsMessage()
    {
        var failure = CreateStatusValidator().Validate(Valid with { Status = (SampleStatus)2 }).Errors.Should().ContainSingle().Subject;

        failure.CustomState.Should().BeSameAs(CommonErrors.InvalidCode);
        failure.ErrorMessage.Should().Be(CommonErrors.InvalidCode.Message);
        failure.PropertyName.Should().Be(nameof(CodeRequest.Status));
    }

    // ---- 실패 ----

    [Fact]
    public void MustBeDefinedEnum_NonFlagsZero_IsRejectedBecauseZeroIsReserved()
    {
        // Unknown(0)은 정의된 멤버지만 예약 값이라 업무 값으로 받지 않는다(coding-conventions "코드값 규칙").
        var result = CreateStatusValidator().Validate(Valid with { Status = SampleStatus.Unknown });

        result.Errors.Should().ContainSingle().Which.CustomState.Should().BeSameAs(CommonErrors.InvalidCode);
    }

    [Theory]
    [MemberData(nameof(UndefinedStatuses))]
    public void MustBeDefinedEnum_UndefinedCode_IsRejectedWith1002(short status)
    {
        var result = CreateStatusValidator().Validate(Valid with { Status = (SampleStatus)status });

        result.Errors.Should().ContainSingle().Which.CustomState.Should().BeSameAs(CommonErrors.InvalidCode);
    }

    [Theory]
    [MemberData(nameof(UndefinedChannelBits))]
    public void MustBeDefinedEnum_FlagsWithUndefinedBit_IsRejectedWith1002(int channels)
    {
        var result = CreateChannelsValidator().Validate(Valid with { Channels = (SampleChannels)channels });

        result.Errors.Should().ContainSingle().Which.CustomState.Should().BeSameAs(CommonErrors.InvalidCode);
    }

    [Fact]
    public void MustBeDefinedEnum_NullRule_ThrowsArgumentNullException()
    {
        IRuleBuilder<CodeRequest, SampleStatus> rule = null!;

        var act = () => rule.MustBeDefinedEnum();

        act.Should().Throw<ArgumentNullException>().WithParameterName("rule");
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData(WideSampleChannels.First)]
    [InlineData(WideSampleChannels.Last)]
    [InlineData(WideSampleChannels.First | WideSampleChannels.Last)]
    public void MustBeDefinedEnum_SignBitFlags_AcceptsDefinedBitsIncludingBit63(WideSampleChannels wide)
    {
        var result = CreateWideValidator().Validate(Valid with { Wide = wide });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(1L << 1)]
    [InlineData(1L << 62)]
    [InlineData(-1L)]
    public void MustBeDefinedEnum_SignBitFlagsWithUndefinedBit_IsRejected(long wide)
    {
        var result = CreateWideValidator().Validate(Valid with { Wide = (WideSampleChannels)wide });

        result.Errors.Should().ContainSingle().Which.CustomState.Should().BeSameAs(CommonErrors.InvalidCode);
    }

    [Fact]
    public void MustBeDefinedEnum_FlagsZeroWithoutNoneMember_PassesAsEmptySet()
    {
        // 조합 규칙(예: 최소 하나)은 Aggregate / Value Object가 검증한다(coding-conventions "비트 마스킹"). 이 규칙은 정의 여부만 본다.
        var result = CreateWideValidator().Validate(Valid with { Wide = 0 });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MustBeDefinedEnum_FollowedByWithError_UsesOverriddenError()
    {
        var channelError = Error.Validation(21007, "정의되지 않은 채널입니다.");
        var validator = new InlineValidator<CodeRequest>();
        validator.RuleFor(request => request.Channels).MustBeDefinedEnum().WithError(channelError);

        var failure = validator.Validate(Valid with { Channels = (SampleChannels)8 }).Errors.Should().ContainSingle().Subject;

        failure.CustomState.Should().BeSameAs(channelError);
    }

    [Fact]
    public void MustBeDefinedEnum_InRequestValidator_StopsChainAtFirstFailure()
    {
        var validator = new CodeRequestValidator();

        var result = validator.Validate(Valid with { Status = (SampleStatus)2 });

        result.Errors.Should().ContainSingle().Which.CustomState.Should().BeSameAs(CommonErrors.InvalidCode);
    }

    private static InlineValidator<CodeRequest> CreateStatusValidator()
    {
        var validator = new InlineValidator<CodeRequest>();
        validator.RuleFor(request => request.Status).MustBeDefinedEnum();
        return validator;
    }

    private static InlineValidator<CodeRequest> CreateChannelsValidator()
    {
        var validator = new InlineValidator<CodeRequest>();
        validator.RuleFor(request => request.Channels).MustBeDefinedEnum();
        return validator;
    }

    private static InlineValidator<CodeRequest> CreateWideValidator()
    {
        var validator = new InlineValidator<CodeRequest>();
        validator.RuleFor(request => request.Wide).MustBeDefinedEnum();
        return validator;
    }
}
