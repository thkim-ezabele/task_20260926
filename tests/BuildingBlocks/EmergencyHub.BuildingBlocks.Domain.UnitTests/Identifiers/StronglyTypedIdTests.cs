using System.Reflection;
using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Identifiers;

// S02-T04 · TD-015: 강타입 ID 계약. EF 변환기 공통 등록(BuildingBlocks.Infrastructure)이 이 인터페이스로 ID 형식을 찾는다.
public sealed class StronglyTypedIdTests
{
    // ---- 성공 ----

    [Fact]
    public void Value_OfPositionalRecordStructId_ReturnsWrappedGuid()
    {
        var guid = Guid.NewGuid();

        IStronglyTypedId<SampleId> id = new SampleId(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equals_SameGuid_IdsAreEqualThroughInterface()
    {
        var guid = Guid.NewGuid();
        IStronglyTypedId<SampleId> left = new SampleId(guid);

        left.Equals(new SampleId(guid)).Should().BeTrue();
        left.Equals(new SampleId(Guid.NewGuid())).Should().BeFalse();
    }

    // ---- 계약 형태 ----

    [Fact]
    public void Interface_DeclaresOnlyGetOnlyGuidValue()
    {
        var members = typeof(IStronglyTypedId<>).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        members.Select(member => member.Name).Should().BeEquivalentTo(["Value", "get_Value"]);
        var value = typeof(IStronglyTypedId<>).GetProperty("Value")!;
        value.PropertyType.Should().Be<Guid>();
        value.CanWrite.Should().BeFalse("ID 값은 생성 뒤 바뀌지 않는다");
    }

    [Fact]
    public void TypeParameter_IsSelfReferencingValueType()
    {
        var parameter = typeof(IStronglyTypedId<>).GetGenericArguments().Single();

        parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint).Should().BeTrue();
        // struct 제약은 System.ValueType도 제약 목록에 넣으므로 인터페이스 제약만 골라 본다.
        parameter.GetGenericParameterConstraints().Where(constraint => constraint.IsInterface).Should().ContainSingle()
            .Which.GetGenericTypeDefinition().Should().Be(typeof(IStronglyTypedId<>));
    }

    [Fact]
    public void Interface_ExtendsEquatableOfSelf()
    {
        typeof(IStronglyTypedId<SampleId>).GetInterfaces().Should().Contain(typeof(IEquatable<SampleId>));
    }

    // ---- 엣지 ----

    [Fact]
    public void Value_EmptyGuid_IsKeptAsIsBecauseTransientIdIsJudgedByEntity()
    {
        IStronglyTypedId<SampleId> id = default(SampleId);

        id.Value.Should().Be(Guid.Empty, "빈 ID 판정은 Entity(기본값 = 식별 전)의 몫이고 ID 형식은 값을 감싸기만 한다");
    }
}
