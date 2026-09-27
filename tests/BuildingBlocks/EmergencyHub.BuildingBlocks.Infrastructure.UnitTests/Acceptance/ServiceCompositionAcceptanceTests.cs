using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using EmergencyHub.BuildingBlocks.Infrastructure.Identifiers;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Auditing;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Acceptance;

// PRD-001 FR-06 인수 조건 "ValidateOnBuild / ValidateScopes 상태에서 마커 구현 타입이 모두 Scoped로 해석된다"를
// 서비스 초기화 코드와 같은 조합(AddBuildingBlocksInfrastructure + AddConventionalServices + 쓰기 · 읽기 DbContext + AddUnitOfWork)으로 전수 확인한다.
// IUnitOfWork는 대역이 아니라 실제 등록 확장(AddUnitOfWork, S02-T07)이 등록한다. DbContext는 더미 연결 문자열이라 연결을 열지 않는다.
// 개별 타입 목록(ConventionalServiceCollectionExtensionsTests)이 아니라 두 확장이 추가한 등록 전체를 대상으로 하므로,
// 새 샘플이나 FluentValidation의 부가 등록이 Scoped가 아니거나 해석되지 않으면 여기서 드러난다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class ServiceCompositionAcceptanceTests
{
    private static readonly Assembly SampleAssembly = typeof(ServiceCompositionAcceptanceTests).Assembly;
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    // 확장들이 추가하는 Singleton은 아래뿐이고 나머지는 모두 Scoped여야 한다. 모두 상태가 없거나 등록 뒤 바뀌지 않는다.
    private static readonly Type[] ExpectedSingletons =
    [
        typeof(TimeProvider), // TimeProvider.System 인스턴스(ADR-0017 공통 인프라).
        typeof(ConventionalServicesRegistration), // AddConventionalServices 두 번째 호출 방지 표식.
        typeof(AuditSaveChangesInterceptor), // 상태 없는 감사 인터셉터 한 인스턴스를 모든 쓰기 DbContext에 붙인다(database.md "공통 DbContext 등록").
        typeof(UniqueConstraintErrorRegistry), // 23505 매핑 레지스트리. 등록 뒤 불변(database.md "23505 매핑 레지스트리 계약").
        typeof(IExceptionClassifier), // 상태 없는 분류기. 전역 예외 처리기(Singleton인 ASP.NET IExceptionHandler)가 쓸 수 있게 Singleton(S02-T06 인계).
        typeof(Microsoft.EntityFrameworkCore.Infrastructure.ServiceProviderAccessor), // EF Core AddDbContext가 스스로 추가하는 루트 공급자 접근자(EF 내부 등록).
    ];

    // ---- 성공: 전수 Scoped · 전수 해석 ----

    [Fact]
    public void Compose_ServiceComposition_EveryAddedRegistrationIsScopedExceptDocumentedSingletons()
    {
        var (services, added) = ComposeAndCaptureAdded();

        added.Should().NotBeEmpty();
        added.Where(d => !ExpectedSingletons.Contains(d.ServiceType))
            .Should().OnlyContain(d => d.Lifetime == ServiceLifetime.Scoped);
        added.Where(d => ExpectedSingletons.Contains(d.ServiceType))
            .Select(d => d.ServiceType)
            .Should().BeEquivalentTo(ExpectedSingletons);
        added.Should().NotContain(d => d.Lifetime == ServiceLifetime.Transient);
        services.Should().ContainSingle(d => d.ServiceType == typeof(TimeProvider))
            .Which.ImplementationInstance.Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void Compose_StrictValidation_ResolvesEveryAddedNonKeyedRegistrationInScope()
    {
        var (services, added) = ComposeAndCaptureAdded();
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        // 공허 통과 방지: 마커 구현 · Handler · Validator · 공통 등록이 대상에 모두 들어 있어야 한다.
        var targets = added.Where(d => !d.IsKeyedService && !d.ServiceType.IsGenericTypeDefinition).Select(d => d.ServiceType).Distinct().ToList();
        targets.Should().Contain(
        [
            typeof(ISampleRepository),
            typeof(ISampleReadRepository),
            typeof(ISampleService),
            typeof(IInternalSampleService),
            typeof(ICommandHandler<CreateSampleCommand, Guid>),
            typeof(ICommandHandler<ArchiveSampleCommand, Unit>),
            typeof(IQueryHandler<GetSampleQuery, string>),
            typeof(FluentValidation.IValidator<CreateSampleCommand>),
            typeof(ISender),
            typeof(IIdGenerator),
            typeof(TimeProvider),
            typeof(IUnitOfWork),
            typeof(IExceptionClassifier),
            typeof(SampleWriteDbContext),
            typeof(SampleReadDbContext),
            typeof(UniqueConstraintErrorRegistry),
        ]);

        var unresolved = targets.Where(type => scope.ServiceProvider.GetService(type) is null).ToList();
        unresolved.Should().BeEmpty();
    }

    [Fact]
    public void Compose_ScopedRegistrations_ResolvedFromRootProvider_ThrowBecauseValidateScopes()
    {
        // Scoped가 루트(사실상 Singleton 수명)로 새어 나가는 의존 역전을 ValidateScopes가 막는지 확인한다.
        var (services, _) = ComposeAndCaptureAdded();
        using var provider = services.BuildServiceProvider(StrictOptions);

        Type[] scopedServices =
        [
            typeof(ISampleService),
            typeof(ICommandHandler<CreateSampleCommand, Guid>),
            typeof(IQueryHandler<GetSampleQuery, string>),
            typeof(ISender),
            typeof(IIdGenerator),
            typeof(IUnitOfWork),
            typeof(SampleWriteDbContext),
            typeof(SampleReadDbContext),
        ];

        var leaked = scopedServices.Where(type => !ThrowsFromRoot(provider, type)).ToList();
        leaked.Should().BeEmpty();
    }

    [Fact]
    public void Compose_TimeProvider_SameSystemInstanceInRootAndEveryScope()
    {
        var (services, _) = ComposeAndCaptureAdded();
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
        first.ServiceProvider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
        second.ServiceProvider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }

    // ---- 엣지: 등록 순서 ----

    [Fact]
    public void Compose_ConventionalBeforeInfrastructure_StrictBuildResolvesSameCommonServices()
    {
        // 서비스 초기화 코드가 두 확장을 어떤 순서로 불러도 결과가 같아야 한다(둘 다 TryAdd로 공통 등록).
        var services = CreateBaseServices();
        services.AddConventionalServices(SampleAssembly);
        services.AddBuildingBlocksInfrastructure();
        AddPersistence(services);

        services.Count(d => d.ServiceType == typeof(ISender)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(TimeProvider)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(IIdGenerator)).Should().Be(1);

        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IIdGenerator>().Should().BeOfType<UuidV7IdGenerator>();
        HandlerChain.Unwrap(scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateSampleCommand, Guid>>())
            .Select(t => t.Name)
            .Should().Equal(
                "LoggingCommandHandlerDecorator`2",
                "ValidationCommandHandlerDecorator`2",
                "TransactionCommandHandlerDecorator`2",
                nameof(CreateSampleCommandHandler));
    }

    [Fact]
    public void Compose_IdGeneratorFromComposedProvider_ProducesVersion7IdsOrderedAcrossScopes()
    {
        var (services, _) = ComposeAndCaptureAdded();
        using var provider = services.BuildServiceProvider(StrictOptions);

        var ids = Enumerable.Range(0, 100)
            .Select(_ =>
            {
                using var scope = provider.CreateScope();
                return scope.ServiceProvider.GetRequiredService<IIdGenerator>().NewId().ToString("D");
            })
            .ToList();

        ids.Should().OnlyContain(id => id[14] == '7');
        ids.Zip(ids.Skip(1)).Should().OnlyContain(pair => string.CompareOrdinal(pair.First, pair.Second) < 0);
    }

    private static bool ThrowsFromRoot(ServiceProvider provider, Type serviceType)
    {
        try
        {
            provider.GetRequiredService(serviceType);
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static ServiceCollection CreateBaseServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        services.AddScoped<IPipelineProbe, CountingPipelineProbe>();
        return services;
    }

    private static (ServiceCollection Services, IReadOnlyList<ServiceDescriptor> Added) ComposeAndCaptureAdded()
    {
        var services = CreateBaseServices();
        var before = services.ToHashSet();

        services.AddBuildingBlocksInfrastructure();
        services.AddConventionalServices(SampleAssembly);
        AddPersistence(services);

        return (services, [.. services.Where(d => !before.Contains(d))]);
    }

    private static void AddPersistence(ServiceCollection services)
    {
        services.AddWriteDbContext<SampleWriteDbContext>(SampleDbContexts.DummyConnectionString);
        services.AddReadDbContext<SampleReadDbContext>(SampleDbContexts.DummyConnectionString);
        services.AddUnitOfWork<SampleWriteDbContext>(SampleUniqueConstraintErrors.Configure);
    }
}
