using System.Reflection;
using EmergencyHub.BuildingBlocks.Domain.Entities;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Events;
using EmergencyHub.BuildingBlocks.Domain.Repositories;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Acceptance;

// PRD-001 FR-04 인수 조건: "에러 코드의 유형 자리와 ErrorType이 일치함을 검증한다. 프레임워크 패키지를 참조하지 않는다."
// 개별 동작의 성공 / 실패 / 엣지는 단위 테스트(Entities · Errors · Results)가 검증하고, 여기서는 어셈블리 단위로 인수 조건만 확인한다.
[Trait("FR", "PRD-001/FR-04")]
public sealed class DomainAssemblyAcceptanceTests
{
    private static readonly Assembly DomainAssembly = typeof(Error).Assembly;

    [Fact]
    public void ReferencedAssemblies_OfDomainAssembly_AreOnlySystemOrNetStandard()
    {
        var referencedNames = DomainAssembly.GetReferencedAssemblies().Select(reference => reference.Name!).ToList();

        referencedNames.Should().NotBeEmpty();
        referencedNames.Should().OnlyContain(
            name => IsBaseClassLibrary(name),
            "Domain은 프레임워크 패키지를 참조하지 않는다(FR-04, ADR-0003). 실제 참조: {0}",
            string.Join(", ", referencedNames));
    }

    [Fact]
    public void ErrorMembers_AcrossDomainAssembly_HaveTypeDigitEqualToErrorTypeValueDividedByTen()
    {
        var errors = DomainAssembly.GetExportedTypes().SelectMany(PublicStaticErrors).ToList();

        errors.Should().NotBeEmpty("공통 에러 코드(CommonErrors)가 Domain 어셈블리에 있어야 한다");
        errors.Should().AllSatisfy(entry =>
        {
            entry.Error.Type.Should().NotBe(ErrorType.None, "{0}: None은 예약 값이다", entry.Name);
            (entry.Error.Code / 1000 % 10).Should().Be(
                (short)entry.Error.Type / 10,
                "{0}({1})의 유형 자리는 ErrorType.{2} / 10이어야 한다",
                entry.Name,
                entry.Error.Code,
                entry.Error.Type);
        });
    }

    [Fact]
    public void ExportedTypes_OfDomainAssembly_ProvideFr04BuildingBlocks()
    {
        var exported = DomainAssembly.GetExportedTypes();

        exported.Should().Contain(
        [
            typeof(Entity<>),
            typeof(AggregateRoot<>),
            typeof(IDomainEvent),
            typeof(Error),
            typeof(ErrorType),
            typeof(Result),
            typeof(Result<>),
            typeof(IRepository),
        ]);
        typeof(IDomainEvent).GetMembers().Should().BeEmpty("IDomainEvent는 마커 인터페이스다");
        typeof(IRepository).GetMembers().Should().BeEmpty("IRepository는 마커 인터페이스다");
    }

    [Fact]
    public void AggregateRoot_PublicSurface_OnlyCollectsDomainEventsWithoutDispatch()
    {
        const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var memberNames = typeof(AggregateRoot<>)
            .GetMembers(DeclaredPublicInstance)
            .Where(member => member is not ConstructorInfo)
            .Select(member => member.Name);

        memberNames.Should().BeEquivalentTo(
            ["DomainEvents", "get_DomainEvents", "ClearDomainEvents"],
            "도메인 이벤트는 수집까지만 하고 디스패치는 이후 토픽이다(FR-04, Q15)");
    }

    [Fact]
    public void Scenario_AggregateWithGivenIdRaisesEventAndFailureFlowsAsResult_BehavesAsSpecified()
    {
        var givenId = SampleId.New();
        var aggregate = new SampleAggregate(givenId);
        aggregate.Happen(new SampleDomainEvent(1));
        Result<SampleAggregate> failure = CommonErrors.NotFound;

        Result<SampleAggregate> success = aggregate;

        success.IsSuccess.Should().BeTrue();
        success.Value.Id.Should().Be(givenId, "Aggregate ID는 밖에서 받은 값을 그대로 쓴다");
        success.Value.DomainEvents.Should().ContainSingle().Which.Should().Be(new SampleDomainEvent(1));
        failure.IsFailure.Should().BeTrue();
        failure.Error.Should().Be(CommonErrors.NotFound);
    }

    private static bool IsBaseClassLibrary(string name) =>
        name is "netstandard" or "mscorlib" or "System"
        || name.StartsWith("System.", StringComparison.Ordinal);

    private static IEnumerable<(string Name, Error Error)> PublicStaticErrors(Type type)
    {
        const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        if (type.ContainsGenericParameters)
        {
            return [];
        }

        var fields = type.GetFields(PublicStatic)
            .Where(field => typeof(Error).IsAssignableFrom(field.FieldType))
            .Select(field => ($"{type.Name}.{field.Name}", (Error)field.GetValue(null)!));
        var properties = type.GetProperties(PublicStatic)
            .Where(property => typeof(Error).IsAssignableFrom(property.PropertyType) && property.GetIndexParameters().Length == 0)
            .Select(property => ($"{type.Name}.{property.Name}", (Error)property.GetValue(null)!));

        return fields.Concat(properties);
    }
}
