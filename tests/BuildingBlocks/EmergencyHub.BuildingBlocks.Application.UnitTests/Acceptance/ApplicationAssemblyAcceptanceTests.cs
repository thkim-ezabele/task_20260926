using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using EmergencyHub.BuildingBlocks.Application.Services;
using EmergencyHub.BuildingBlocks.Application.Validation;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Acceptance;

// PRD-001 FR-05 중 S02-T01 · T02 범위(계약 · 디스패처 · 데코레이터)의 어셈블리 단위 확인. 파이프라인 순서는 S02-T03이 검증한다.
// 레이어 의존 규칙 전체는 S02-T05 아키텍처 테스트가 원본이고, 여기서는 이 작업이 만든 참조만 확인한다.
[Trait("FR", "PRD-001/FR-05")]
public sealed class ApplicationAssemblyAcceptanceTests
{
    private static readonly Assembly ApplicationAssembly = typeof(ISender).Assembly;

    [Fact]
    public void ExportedTypes_OfApplicationAssembly_ProvideT01Contracts()
    {
        ApplicationAssembly.GetExportedTypes().Should().Contain(
        [
            typeof(Unit),
            typeof(ICommand),
            typeof(ICommand<>),
            typeof(IQuery<>),
            typeof(ICommandHandler<>),
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>),
            typeof(ISender),
            typeof(IUnitOfWork),
            typeof(IReadRepository),
            typeof(IService),
            typeof(IIdGenerator),
            typeof(IExceptionClassifier),
        ]);
    }

    [Fact]
    public void ExportedTypes_OfApplicationAssembly_ProvideT02ValidationContracts()
    {
        // Validator 작성에 쓰는 공통 기반과 WithError 확장만 공개한다. 데코레이터는 internal(ADR-0015).
        ApplicationAssembly.GetExportedTypes().Should().Contain([typeof(RequestValidator<>), typeof(ValidationRuleExtensions)]);
        typeof(RequestValidator<>).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void Implementations_OfApplicationAssembly_AreInternalSealed()
    {
        Type[] implementations =
        [
            typeof(Sender),
            typeof(RequestInvokerCache),
            typeof(LoggingCommandHandlerDecorator<,>),
            typeof(LoggingQueryHandlerDecorator<,>),
            typeof(ValidationCommandHandlerDecorator<,>),
            typeof(ValidationQueryHandlerDecorator<,>),
            typeof(TransactionCommandHandlerDecorator<,>),
        ];

        implementations.Should().AllSatisfy(type =>
        {
            type.IsPublic.Should().BeFalse("{0}는 등록 진입점(S02-T03)으로만 노출한다", type.Name);
            type.IsSealed.Should().BeTrue();
        });
    }

    [Fact]
    public void ReferencedAssemblies_OfApplicationAssembly_AreOnlyBaseLibraryDomainExtensionsAbstractionsOrFluentValidation()
    {
        var referencedNames = ApplicationAssembly.GetReferencedAssemblies().Select(reference => reference.Name!).ToList();

        referencedNames.Should().Contain("EmergencyHub.BuildingBlocks.Domain");
        referencedNames.Should().OnlyContain(
            name => IsAllowed(name),
            "Application은 FluentValidation 외 EF Core · Scrutor · ASP.NET Core를 참조하지 않는다(ADR-0015, ADR-0018, ADR-0024). 실제 참조: {0}",
            string.Join(", ", referencedNames));
    }

    private static bool IsAllowed(string name) =>
        name is "netstandard" or "mscorlib" or "System" or "EmergencyHub.BuildingBlocks.Domain" or "FluentValidation"
        || name.StartsWith("System.", StringComparison.Ordinal)
        || (name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) && name.EndsWith(".Abstractions", StringComparison.Ordinal));
}
