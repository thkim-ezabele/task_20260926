using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04 A6: 체크 제약 도우미는 enum 기반 형식을 검사한다. 코드값은 short, [Flags]는 int / long이 아니면 모델 생성(매핑 호출) 시점에 예외.
// 이름과 SQL은 명명 규칙이 끝난 뒤 공통 규칙이 만든다(CommonModelMetadataTests A4 · A5).
[Trait("FR", "PRD-001/FR-06")]
public sealed class PropertyBuilderExtensionsTests
{
    private readonly EntityTypeBuilder<EnumPropertyHolder> _holder = new ModelBuilder().Entity<EnumPropertyHolder>();

    // ---- 성공 ----

    [Fact]
    public void HasCodeCheckConstraint_ShortCodeEnum_ReturnsSameBuilderForChaining()
    {
        var builder = _holder.Property(holder => holder.OrderStatus);

        builder.HasCodeCheckConstraint().Should().BeSameAs(builder);
    }

    [Fact]
    public void HasCodeCheckConstraint_NullableShortCodeEnum_ReturnsSameBuilder()
    {
        var builder = _holder.Property(holder => holder.NullableOrderStatus);

        builder.HasCodeCheckConstraint().Should().BeSameAs(builder);
    }

    [Fact]
    public void HasFlagsCheckConstraint_IntFlagsEnum_ReturnsSameBuilderForChaining()
    {
        var builder = _holder.Property(holder => holder.DeliveryChannels);

        builder.HasFlagsCheckConstraint().Should().BeSameAs(builder);
    }

    [Fact]
    public void HasFlagsCheckConstraint_NullableIntFlagsEnum_ReturnsSameBuilder()
    {
        var builder = _holder.Property(holder => holder.NullableDeliveryChannels);

        builder.HasFlagsCheckConstraint().Should().BeSameAs(builder);
    }

    [Fact]
    public void HasFlagsCheckConstraint_LongFlagsEnum_IsAccepted()
    {
        var act = () => _holder.Property(holder => holder.WideChannels).HasFlagsCheckConstraint();

        act.Should().NotThrow();
    }

    // ---- 실패: 기반 형식 ----

    [Fact]
    public void HasCodeCheckConstraint_IntBackedCodeEnum_ThrowsNamingTypeAndRequiredShort()
    {
        var act = () => _holder.Property(holder => holder.IntBackedStatus).HasCodeCheckConstraint();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(IntBackedStatus)}*short*");
    }

    [Fact]
    public void HasFlagsCheckConstraint_ShortBackedFlagsEnum_ThrowsNamingTypeAndRequiredIntOrLong()
    {
        var act = () => _holder.Property(holder => holder.ShortBackedChannels).HasFlagsCheckConstraint();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(ShortBackedChannels)}*int*long*");
    }

    // ---- 실패: 코드 / 플래그 구분 ----

    [Fact]
    public void HasCodeCheckConstraint_FlagsEnum_ThrowsBecauseCombinationsNeedMaskRule()
    {
        var act = () => _holder.Property(holder => holder.FlaggedCodeStatus).HasCodeCheckConstraint();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(FlaggedCodeStatus)}*Flags*");
    }

    [Fact]
    public void HasFlagsCheckConstraint_EnumWithoutFlagsAttribute_Throws()
    {
        var act = () => _holder.Property(holder => holder.PlainIntCode).HasFlagsCheckConstraint();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(PlainIntCode)}*Flags*");
    }

    // ---- 엣지 ----

    [Fact]
    public void HasCodeCheckConstraint_EnumWithOnlyReservedZero_ThrowsBecauseAllowedListIsEmpty()
    {
        var act = () => _holder.Property(holder => holder.UnknownOnlyStatus).HasCodeCheckConstraint();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(UnknownOnlyStatus)}*");
    }

    [Fact]
    public void HasFlagsCheckConstraint_FlagsWithoutAnyBit_ThrowsBecauseMaskIsZero()
    {
        var act = () => _holder.Property(holder => holder.NoBitChannels).HasFlagsCheckConstraint();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(NoBitChannels)}*");
    }

    [Fact]
    public void HasFlagsCheckConstraint_FlagUsingSignBit_ThrowsBecauseStoredValueWouldBeNegative()
    {
        var act = () => _holder.Property(holder => holder.SignBitChannels).HasFlagsCheckConstraint();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(SignBitChannels)}*");
    }
}
