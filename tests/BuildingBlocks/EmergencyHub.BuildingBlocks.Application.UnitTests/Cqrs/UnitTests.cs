using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Cqrs;

public sealed class UnitTests
{
    [Fact]
    public void Value_ComparedWithDefault_IsEqual()
    {
        Unit.Value.Should().Be(default(Unit));
        Unit.Value.Should().Be(new Unit());
    }

    [Fact]
    public void Value_ReadTwice_HasSameHashCodeAndEquality()
    {
        var first = Unit.Value;
        var second = Unit.Value;

        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void UnitType_IsReadOnlyValueType()
    {
        typeof(Unit).IsValueType.Should().BeTrue();
        typeof(Unit).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .Should().BeEmpty("Unit은 값이 없음을 나타내므로 상태를 가지지 않는다");
    }
}
