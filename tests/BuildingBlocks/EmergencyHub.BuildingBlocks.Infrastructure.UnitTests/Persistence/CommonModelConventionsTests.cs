using EmergencyHub.BuildingBlocks.Domain.Identifiers;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04 · TD-015: 강타입 ID 형식 검색(ConfigureConventions 변환기 등록 대상)과 OnModelCreating 뒤처리(키 ValueGeneratedNever, shadow property).
[Trait("FR", "PRD-001/FR-06")]
public sealed class CommonModelConventionsTests
{
    [Fact]
    public void FindStronglyTypedIdTypes_SampleAssembly_FindsSelfImplementingValueTypes()
    {
        var types = CommonModelConventions.FindStronglyTypedIdTypes([typeof(OrderId).Assembly]);

        types.Should().Contain([typeof(OrderId), typeof(CustomerId), typeof(IdWithoutGuidConstructor)]);
        types.Should().OnlyContain(type => type.IsValueType && typeof(IStronglyTypedId<>).MakeGenericType(type).IsAssignableFrom(type));
    }

    [Fact]
    public void FindStronglyTypedIdTypes_ExcludesEnumsRecordsAndOtherStructs()
    {
        var types = CommonModelConventions.FindStronglyTypedIdTypes([typeof(OrderId).Assembly]);

        types.Should().NotContain([typeof(OrderStatus), typeof(GeoPoint), typeof(Order)]);
    }

    [Fact]
    public void FindStronglyTypedIdTypes_SameAssemblyTwice_ReturnsEachTypeOnce()
    {
        var assembly = typeof(OrderId).Assembly;

        var types = CommonModelConventions.FindStronglyTypedIdTypes([assembly, assembly]);

        types.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void FindStronglyTypedIdTypes_NoAssemblies_ReturnsEmpty()
    {
        CommonModelConventions.FindStronglyTypedIdTypes([]).Should().BeEmpty();
    }

    [Fact]
    public void FindStronglyTypedIdTypes_AssemblyWithoutIds_ReturnsEmpty()
    {
        CommonModelConventions.FindStronglyTypedIdTypes([typeof(object).Assembly]).Should().BeEmpty();
    }

    [Fact]
    public void BuildModel_MappingSetsIdKeyValueGeneratedOnAdd_ForcesNever()
    {
        // 규칙 없는 ModelBuilder: 서비스 매핑과 공통 뒤처리만 적용된다.
        var modelBuilder = new ModelBuilder();

        CommonModelConventions.BuildModel(modelBuilder, new OnAddKeyModelDefinition());

        modelBuilder.Model.FindEntityType(typeof(Order))!.FindPrimaryKey()!.Properties.Single()
            .ValueGenerated.Should().Be(ValueGenerated.Never, "ID는 Handler가 IIdGenerator로 만든다(ADR-0013)");
    }

    [Fact]
    public void BuildModel_AppliesServiceMappingBeforeAddingShadowProperties()
    {
        var modelBuilder = new ModelBuilder();

        CommonModelConventions.BuildModel(modelBuilder, new OnAddKeyModelDefinition());

        var order = modelBuilder.Model.FindEntityType(typeof(Order))!;
        order.FindProperty(ShadowPropertyNames.CreatedAt).Should().NotBeNull("뒤처리는 매핑이 만든 엔티티 형식에 적용된다");
        order.FindProperty(ShadowPropertyNames.Version)!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void BuildModel_NullArguments_Throw()
    {
        var withoutBuilder = () => CommonModelConventions.BuildModel(null!, new OnAddKeyModelDefinition());
        var withoutDefinition = () => CommonModelConventions.BuildModel(new ModelBuilder(), null!);

        withoutBuilder.Should().Throw<ArgumentNullException>();
        withoutDefinition.Should().Throw<ArgumentNullException>();
    }
}
