using EmergencyHub.BuildingBlocks.Domain.Identifiers;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// 강타입 ID: IStronglyTypedId<EmployeeId>(공통 값 변환기 · ValueGeneratedNever 대상, TD-015), Guid 한 개짜리 public 생성자.
public sealed class EmployeeIdTests
{
    [Fact]
    public void Type_IsValueTypeImplementingStronglyTypedIdOfItselfWithGuidConstructor()
    {
        typeof(EmployeeId).IsValueType.Should().BeTrue();
        typeof(IStronglyTypedId<EmployeeId>).IsAssignableFrom(typeof(EmployeeId)).Should().BeTrue();
        typeof(EmployeeId).GetConstructor([typeof(Guid)]).Should().NotBeNull();
    }

    [Fact]
    public void Equality_SameGuid_AreEqual()
    {
        var value = Guid.Parse("0192a1b3-0000-7000-8000-000000000001");

        new EmployeeId(value).Should().Be(new EmployeeId(value));
        new EmployeeId(value).Value.Should().Be(value);
    }

    [Fact]
    public void Equality_DifferentGuid_AreNotEqual()
    {
        new EmployeeId(Guid.Parse("0192a1b3-0000-7000-8000-000000000001"))
            .Should().NotBe(new EmployeeId(Guid.Parse("0192a1b3-0000-7000-8000-000000000002")));
    }

    [Fact]
    public void Default_WrapsEmptyGuid()
    {
        default(EmployeeId).Value.Should().Be(Guid.Empty);
    }
}
