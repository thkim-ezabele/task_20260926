using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04 · TD-015: 강타입 ID ↔ uuid 변환기. 식 트리에서 static abstract 멤버를 부를 수 없어(CS8927) Guid 생성자로 식을 만든다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class StronglyTypedIdValueConverterTests
{
    // ---- 성공 ----

    [Fact]
    public void ConvertToProvider_Id_ReturnsWrappedGuid()
    {
        var guid = Guid.NewGuid();
        var converter = new StronglyTypedIdValueConverter<OrderId>();

        converter.ConvertToProvider(new OrderId(guid)).Should().Be(guid);
    }

    [Fact]
    public void ConvertFromProvider_Guid_ReturnsIdWithSameValue()
    {
        var guid = Guid.NewGuid();
        var converter = new StronglyTypedIdValueConverter<OrderId>();

        converter.ConvertFromProvider(guid).Should().Be(new OrderId(guid));
    }

    [Fact]
    public void ProviderClrType_IsGuid()
    {
        var converter = new StronglyTypedIdValueConverter<OrderId>();

        converter.ModelClrType.Should().Be<OrderId>();
        converter.ProviderClrType.Should().Be<Guid>();
    }

    // ---- 엣지 ----

    [Fact]
    public void RoundTrip_EmptyGuid_IsPreserved()
    {
        var converter = new StronglyTypedIdValueConverter<OrderId>();

        converter.ConvertFromProvider(converter.ConvertToProvider(new OrderId(Guid.Empty))).Should().Be(new OrderId(Guid.Empty));
    }

    // ---- 실패 ----

    [Fact]
    public void Constructor_IdWithoutGuidConstructor_ThrowsNamingTheType()
    {
        var act = () => new StronglyTypedIdValueConverter<IdWithoutGuidConstructor>();

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(IdWithoutGuidConstructor)}*Guid*");
    }
}
