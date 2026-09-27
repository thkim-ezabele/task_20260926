using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using EmergencyHub.BuildingBlocks.Infrastructure.Identifiers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Acceptance;

// PRD-001 FR-06 인수 조건 "ValidateOnBuild / ValidateScopes 상태에서 마커 구현 타입이 모두 Scoped로 해석된다"를
// 서비스 초기화 코드와 같은 조합(AddBuildingBlocksInfrastructure + AddConventionalServices)으로 전수 확인한다.
// 개별 타입 목록(ConventionalServiceCollectionExtensionsTests)이 아니라 두 확장이 추가한 등록 전체를 대상으로 하므로,
// 새 샘플이나 FluentValidation의 부가 등록이 Scoped가 아니거나 해석되지 않으면 여기서 드러난다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class ServiceCompositionAcceptanceTests
{
    private static readonly Assembly SampleAssembly = typeof(ServiceCompositionAcceptanceTests).Assembly;
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    // 두 확장이 추가하는 Singleton은 이 둘뿐이다(TimeProvider.System, 두 번째 호출 방지 표식). 나머지는 모두 Scoped여야 한다.
    private static readonly Type[] ExpectedSingletons = [typeof(TimeProvider), typeof(ConventionalServicesRegistration)];

    // ---- 성공: 전수 Scoped · 전수 해석 ----

    [Fact]
    public void Compose_InfrastructureAndConventional_EveryAddedRegistrationIsScopedExceptTimeProviderAndMarker()
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
        services.AddScoped<IUnitOfWork, CountingUnitOfWork>();
        return services;
    }

    private static (ServiceCollection Services, IReadOnlyList<ServiceDescriptor> Added) ComposeAndCaptureAdded()
    {
        var services = CreateBaseServices();
        var before = services.ToHashSet();

        services.AddBuildingBlocksInfrastructure();
        services.AddConventionalServices(SampleAssembly);

        return (services, [.. services.Where(d => !before.Contains(d))]);
    }
}
