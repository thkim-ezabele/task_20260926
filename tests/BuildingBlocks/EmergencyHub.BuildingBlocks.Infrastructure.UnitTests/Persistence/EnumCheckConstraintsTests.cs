using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04: ck_ 이름 · SQL 생성 규칙(database.md "코드값 규칙" · "비트 마스킹 규칙").
// 코드값: col IN (정의 값, 0 제외, 중복 없이 오름차순). [Flags]: col >= 0 AND (col & ~mask) = 0, mask = 정의 값 전체 OR.
[Trait("FR", "PRD-001/FR-06")]
public sealed class EnumCheckConstraintsTests
{
    // ---- 성공 ----

    [Fact]
    public void CodeSql_ShortEnum_ListsDefinedValuesExceptZeroAscending()
    {
        EnumCheckConstraints.CodeSql("order_status", typeof(OrderStatus)).Should().Be("order_status IN (1, 2, 3)");
    }

    [Fact]
    public void FlagsSql_IntFlags_UsesMaskOfAllDefinedBits()
    {
        EnumCheckConstraints.FlagsSql("delivery_channels", typeof(DeliveryChannels))
            .Should().Be("delivery_channels >= 0 AND (delivery_channels & ~11) = 0");
    }

    [Fact]
    public void Name_TableAndColumn_IsCkTableColumn()
    {
        EnumCheckConstraints.Name("orders", "order_status").Should().Be("ck_orders_order_status");
    }

    // ---- 엣지 ----

    [Fact]
    public void CodeSql_AliasAndNegativeValues_AreDistinctAndAscending()
    {
        EnumCheckConstraints.CodeSql("status", typeof(AliasedStatus)).Should().Be("status IN (-1, 1, 2)");
    }

    [Fact]
    public void FlagsSql_LongFlagsBeyond32Bits_WritesFullMask()
    {
        EnumCheckConstraints.FlagsSql("channels", typeof(WideChannels))
            .Should().Be("channels >= 0 AND (channels & ~4611687117939015681) = 0");
    }

    [Fact]
    public void FlagsSql_MaskWithGap_DoesNotUseRangeCondition()
    {
        var sql = EnumCheckConstraints.FlagsSql("delivery_channels", typeof(DeliveryChannels));

        // 범위 조건(<= 11)은 빈 비트 자리 값 4를 통과시킨다. 마스크 조건만 있어야 한다.
        sql.Should().NotContain("<=");
        sql.Should().Contain("& ~11");
    }

    [Fact]
    public void Name_Exactly63Bytes_IsAccepted()
    {
        var column = new string('c', 63 - "ck_t_".Length);

        EnumCheckConstraints.Name("t", column).Should().HaveLength(63);
    }

    // ---- 실패 ----

    [Fact]
    public void Name_Over63Bytes_ThrowsBecausePostgreSqlWouldTruncateSilently()
    {
        var column = new string('c', 64 - "ck_t_".Length);

        var act = () => EnumCheckConstraints.Name("t", column);

        act.Should().Throw<InvalidOperationException>().WithMessage("*63*");
    }
}
