using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using EmergencyHub.BuildingBlocks.Application.Services;
using EmergencyHub.BuildingBlocks.Domain.Repositories;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrutor;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.DependencyInjection;

// PRD-001 FR-06 · ADR-0010 · ADR-0017: AddConventionalServices(Scrutor Scan + TryDecorate, FluentValidation 검색).
// 검색 대상은 이 테스트 어셈블리(규칙을 지키는 예시만 있음). 위반 예시는 DynamicSampleAssembly로 따로 만든다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class ConventionalServiceCollectionExtensionsTests
{
    private static readonly Assembly SampleAssembly = typeof(ConventionalServiceCollectionExtensionsTests).Assembly;
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    private readonly IPipelineProbe _probe = Substitute.For<IPipelineProbe>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // ---- 성공: 마커 구현 타입 ----

    [Theory]
    [InlineData(typeof(ISampleRepository), typeof(SampleRepository))]
    [InlineData(typeof(ISampleReadRepository), typeof(SampleReadRepository))]
    [InlineData(typeof(ISampleService), typeof(SampleService))]
    public void AddConventionalServices_MarkerImplementation_RegistersServiceInterfaceAsScoped(Type serviceType, Type implementationType)
    {
        var services = CreateServices(SampleAssembly);

        var descriptor = services.Should().ContainSingle(d => d.ServiceType == serviceType).Subject;
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(implementationType);
    }

    [Fact]
    public void AddConventionalServices_InternalImplementation_IsRegistered()
    {
        var services = CreateServices(SampleAssembly);

        services.Should().ContainSingle(d => d.ServiceType == typeof(IInternalSampleService))
            .Which.ImplementationType!.Name.Should().Be("InternalSampleService");
    }

    [Fact]
    public void BuildServiceProvider_StrictValidation_ResolvesEveryMarkerServiceInScope()
    {
        using var provider = CreateServices(SampleAssembly).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISampleRepository>().Should().BeOfType<SampleRepository>();
        scope.ServiceProvider.GetRequiredService<ISampleReadRepository>().Should().BeOfType<SampleReadRepository>();
        scope.ServiceProvider.GetRequiredService<IInternalSampleService>().Should().NotBeNull();
        var service = scope.ServiceProvider.GetRequiredService<ISampleService>().Should().BeOfType<SampleService>().Subject;
        service.Repository.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<ISampleRepository>());
    }

    [Fact]
    public void ResolveMarkerService_SameScopeAndOtherScope_SameInstanceInScopeAndNewInstanceInOtherScope()
    {
        using var provider = CreateServices(SampleAssembly).BuildServiceProvider(StrictOptions);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<ISampleService>();

        a.Should().BeSameAs(first.ServiceProvider.GetRequiredService<ISampleService>());
        a.Should().NotBeSameAs(second.ServiceProvider.GetRequiredService<ISampleService>());
    }

    // ---- 성공: Validator ----

    [Theory]
    [InlineData(typeof(IValidator<CreateSampleCommand>))]
    [InlineData(typeof(IValidator<GetSampleQuery>))]
    public void AddConventionalServices_Validator_RegisteredAsScopedIncludingInternal(Type validatorType)
    {
        var services = CreateServices(SampleAssembly);

        services.Should().ContainSingle(d => d.ServiceType == validatorType)
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    // ---- 성공: Handler 등록 ----

    [Theory]
    [InlineData(typeof(ICommandHandler<CreateSampleCommand, Guid>))]
    [InlineData(typeof(ICommandHandler<ArchiveSampleCommand, Unit>))]
    [InlineData(typeof(IQueryHandler<GetSampleQuery, string>))]
    public void AddConventionalServices_Handler_RegisteredOnceAsScopedTwoArgumentInterface(Type handlerType)
    {
        var services = CreateServices(SampleAssembly);

        // Scrutor 7은 감싼 안쪽 단계(원래 Handler와 안쪽 데코레이터)를 같은 ServiceType의 keyed 등록으로 남긴다.
        // 키 없이 해석되는 등록(가장 바깥)은 하나뿐이어야 하고, 안쪽 단계를 포함한 모든 단계가 Scoped여야 한다.
        services.Should().ContainSingle(d => d.ServiceType == handlerType && !d.IsKeyedService)
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        services.Where(d => d.ServiceType == handlerType).Should().OnlyContain(d => d.Lifetime == ServiceLifetime.Scoped);
    }

    [Theory]
    [InlineData(typeof(ICommandHandler<CreateSampleCommand, Guid>), 4)]
    [InlineData(typeof(IQueryHandler<GetSampleQuery, string>), 3)]
    public void AddConventionalServices_Handler_HasOneRegistrationPerPipelineStage(Type handlerType, int expectedStages)
    {
        // Handler 1 + 데코레이터 수(Command 3, Query 2). 한 인자 형태 추가 등록이나 두 겹 감싸기가 있으면 개수가 달라진다.
        var services = CreateServices(SampleAssembly);

        services.Count(d => d.ServiceType == handlerType).Should().Be(expectedStages);
    }

    [Fact]
    public void AddConventionalServices_Called_RegistersApplicationCommonServices()
    {
        var services = CreateServices(SampleAssembly);

        services.Should().ContainSingle(d => d.ServiceType == typeof(ISender)).Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        services.Should().ContainSingle(d => d.ServiceType == typeof(TimeProvider)).Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddConventionalServices_Called_ReturnsSameCollectionForChaining()
    {
        var services = CreateBaseServices();

        services.AddConventionalServices(SampleAssembly).Should().BeSameAs(services);
    }

    // ---- 성공: 해석된 체인 순서(ADR-0015) ----

    [Fact]
    public void ResolveCommandHandler_StrictValidation_ChainIsLoggingValidationTransactionHandler()
    {
        using var provider = CreateServices(SampleAssembly).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateSampleCommand, Guid>>();

        HandlerChain.Unwrap(handler).Should().Equal(
        [
            .. PipelineDecorators.CommandHandlerDecorators.Reverse(),
            typeof(CreateSampleCommandHandler),
        ]);
        HandlerChain.Unwrap(handler).Select(t => t.Name).Should().Equal(
            "LoggingCommandHandlerDecorator`2",
            "ValidationCommandHandlerDecorator`2",
            "TransactionCommandHandlerDecorator`2",
            nameof(CreateSampleCommandHandler));
    }

    [Fact]
    public void ResolveUnitCommandHandler_OneArgumentImplementation_ChainIsLoggingValidationTransactionHandler()
    {
        using var provider = CreateServices(SampleAssembly).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ArchiveSampleCommand, Unit>>();

        HandlerChain.Unwrap(handler).Should().Equal(
        [
            .. PipelineDecorators.CommandHandlerDecorators.Reverse(),
            typeof(ArchiveSampleCommandHandler),
        ]);
    }

    [Fact]
    public void ResolveQueryHandler_StrictValidation_ChainIsLoggingValidationHandlerWithoutTransaction()
    {
        using var provider = CreateServices(SampleAssembly).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetSampleQuery, string>>();

        var chain = HandlerChain.Unwrap(handler);
        chain.Should().Equal(
        [
            .. PipelineDecorators.QueryHandlerDecorators.Reverse(),
            typeof(GetSampleQueryHandler),
        ]);
        chain.Select(t => t.Name).Should().NotContain(name => name.StartsWith("Transaction", StringComparison.Ordinal));
    }

    [Fact]
    public void ResolveCommandHandler_SameScopeAndOtherScope_SameChainInScopeAndNewChainInOtherScope()
    {
        using var provider = CreateServices(SampleAssembly).BuildServiceProvider(StrictOptions);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<ICommandHandler<CreateSampleCommand, Guid>>();
        var b = first.ServiceProvider.GetRequiredService<ICommandHandler<CreateSampleCommand, Guid>>();
        var c = second.ServiceProvider.GetRequiredService<ICommandHandler<CreateSampleCommand, Guid>>();

        a.Should().BeSameAs(b);
        a.Should().NotBeSameAs(c);
    }

    // ---- 실패: 중복 등록 → 시작 실패(RegistrationStrategy.Throw) ----

    [Fact]
    public void AddConventionalServices_TwoImplementationsOfSameServiceInterface_ThrowsDuplicateTypeRegistration()
    {
        var assembly = DynamicSampleAssembly.Define(
            ("FirstDuplicatedService", [typeof(IDuplicatedSampleService)]),
            ("SecondDuplicatedService", [typeof(IDuplicatedSampleService)]));

        var act = () => CreateServices(assembly);

        act.Should().Throw<DuplicateTypeRegistrationException>().WithMessage($"*{nameof(IDuplicatedSampleService)}*");
    }

    [Fact]
    public void AddConventionalServices_ServiceInterfaceAlreadyRegistered_ThrowsDuplicateTypeRegistration()
    {
        var services = CreateBaseServices().AddScoped<ISampleService, SampleService>();

        var act = () => services.AddConventionalServices(SampleAssembly);

        act.Should().Throw<DuplicateTypeRegistrationException>().WithMessage($"*{nameof(ISampleService)}*");
    }

    [Fact]
    public void AddConventionalServices_TwoHandlersForSameCommand_ThrowsDuplicateTypeRegistration()
    {
        var assembly = DynamicSampleAssembly.Define(
            ("FirstOrphanHandler", [typeof(ICommandHandler<OrphanSampleCommand, Unit>)]),
            ("SecondOrphanHandler", [typeof(ICommandHandler<OrphanSampleCommand>)]));

        var act = () => CreateServices(assembly);

        act.Should().Throw<DuplicateTypeRegistrationException>();
    }

    // ---- 실패: 서비스 인터페이스는 구현마다 정확히 하나(ADR-0010) ----

    [Fact]
    public void AddConventionalServices_ClassImplementsTwoServiceInterfaces_ThrowsInvalidOperationWithTypeName()
    {
        var assembly = DynamicSampleAssembly.Define(
            ("PairedService", [typeof(IFirstPairedSampleService), typeof(ISecondPairedSampleService)]));

        var act = () => CreateServices(assembly);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PairedService*")
            .WithMessage($"*{nameof(IFirstPairedSampleService)}*")
            .WithMessage($"*{nameof(ISecondPairedSampleService)}*");
    }

    [Fact]
    public void AddConventionalServices_ClassImplementsMarkerDirectly_ThrowsInvalidOperationWithTypeName()
    {
        var assembly = DynamicSampleAssembly.Define(("MarkerOnlyService", [typeof(IService)]));

        var act = () => CreateServices(assembly);

        act.Should().Throw<InvalidOperationException>().WithMessage("*MarkerOnlyService*");
    }

    [Fact]
    public void AddConventionalServices_ViolationFound_RegistersNothingFromScan()
    {
        // 위반을 먼저 검사하므로 일부만 등록된 컬렉션이 남지 않는다.
        var assembly = DynamicSampleAssembly.Define(("MarkerOnlyRepository", [typeof(IRepository)]));
        var services = CreateBaseServices();
        var before = services.Count;

        var act = () => services.AddConventionalServices(SampleAssembly, assembly);

        act.Should().Throw<InvalidOperationException>();
        services.Count.Should().Be(before);
    }

    // ---- 실패: 인자 ----

    [Fact]
    public void AddConventionalServices_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddConventionalServices(SampleAssembly);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddConventionalServices_NullAssemblies_ThrowsArgumentNullException()
    {
        var act = () => CreateBaseServices().AddConventionalServices(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("assemblies");
    }

    [Fact]
    public void AddConventionalServices_NoAssemblies_ThrowsArgumentException()
    {
        var act = () => CreateBaseServices().AddConventionalServices();

        act.Should().Throw<ArgumentException>().WithParameterName("assemblies");
    }

    [Fact]
    public void AddConventionalServices_NullAssemblyElement_ThrowsArgumentException()
    {
        var act = () => CreateBaseServices().AddConventionalServices(SampleAssembly, null!);

        act.Should().Throw<ArgumentException>().WithParameterName("assemblies");
    }

    // ---- 실패: 두 번 호출하면 데코레이터가 두 겹(커밋 두 번)이 되므로 막는다 ----

    [Fact]
    public void AddConventionalServices_CalledTwice_ThrowsInvalidOperationException()
    {
        var services = CreateServices(SampleAssembly);

        var act = () => services.AddConventionalServices(DynamicSampleAssembly.Define());

        act.Should().Throw<InvalidOperationException>().WithMessage("*AddConventionalServices*");
    }

    [Fact]
    public void ResolveCommandHandler_UnitOfWorkNotRegistered_ThrowsBecauseTransactionDecoratorNeedsIt()
    {
        var services = CreateBaseServices(registerUnitOfWork: false).AddConventionalServices(SampleAssembly);
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateSampleCommand, Guid>>();

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(IUnitOfWork)}*");
    }

    // ---- 엣지: 등록하지 않아야 하는 것 ----

    [Fact]
    public void AddConventionalServices_OneArgumentHandler_NotRegisteredAsOneArgumentInterface()
    {
        // 한 인자 형태로도 등록되면 데코레이터를 우회하는 해석 경로가 생긴다(ADR-0015).
        var services = CreateServices(SampleAssembly);
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        services.Should().NotContain(d => d.ServiceType == typeof(ICommandHandler<ArchiveSampleCommand>));
        scope.ServiceProvider.GetService<ICommandHandler<ArchiveSampleCommand>>().Should().BeNull();
    }

    [Theory]
    [InlineData(typeof(SampleRepository))]
    [InlineData(typeof(SampleService))]
    [InlineData(typeof(CreateSampleCommandHandler))]
    [InlineData(typeof(IRepository))]
    [InlineData(typeof(IReadRepository))]
    [InlineData(typeof(IService))]
    public void AddConventionalServices_ConcreteTypeOrMarker_NotRegisteredAsServiceType(Type type)
    {
        var services = CreateServices(SampleAssembly);

        services.Should().NotContain(d => d.ServiceType == type);
    }

    [Theory]
    [InlineData(typeof(SampleUnitOfWork))]
    [InlineData(typeof(SampleExceptionClassifier))]
    [InlineData(typeof(SampleIdGenerator))]
    public void AddConventionalServices_PortImplementationWithoutMarker_NotRegistered(Type implementationType)
    {
        // IUnitOfWork · IExceptionClassifier · IIdGenerator는 IService를 상속하지 않으므로 자동 등록 대상이 아니다.
        var services = CreateServices(SampleAssembly);

        services.Should().NotContain(d => d.ImplementationType == implementationType);
        services.Should().NotContain(d => d.ServiceType == typeof(IExceptionClassifier));
        services.Should().NotContain(d => d.ServiceType == typeof(IIdGenerator));
    }

    [Fact]
    public void AddConventionalServices_OnlyAbstractImplementation_NotRegistered()
    {
        var services = CreateServices(SampleAssembly);

        services.Should().NotContain(d => d.ServiceType == typeof(IAbstractOnlySampleService));
    }

    // ---- 엣지: 대상 어셈블리 구성 ----

    [Fact]
    public void AddConventionalServices_AssemblyWithoutHandlers_BuildsAndResolvesSender()
    {
        // TryDecorate는 대상 Handler가 없어도 시작을 실패시키지 않는다(ADR-0017).
        var services = CreateServices(DynamicSampleAssembly.Define());

        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        services.Should().NotContain(d => IsHandlerService(d.ServiceType));
        scope.ServiceProvider.GetRequiredService<ISender>().Should().NotBeNull();
    }

    [Fact]
    public void AddConventionalServices_BuildingBlocksApplicationAssembly_DoesNotRegisterOpenGenericDecoratorsAsHandlers()
    {
        var services = CreateServices(typeof(ISender).Assembly);

        using var provider = services.BuildServiceProvider(StrictOptions);

        var applicationAssembly = typeof(ISender).Assembly;
        services.Should().NotContain(d => IsHandlerService(d.ServiceType));
        services.Should().NotContain(d => d.ImplementationType != null && d.ImplementationType.Assembly == applicationAssembly && d.ImplementationType.IsGenericTypeDefinition);
    }

    [Fact]
    public void AddConventionalServices_SameAssemblyTwiceInOneCall_RegistersOnce()
    {
        var services = CreateServices(SampleAssembly, SampleAssembly);

        services.Should().ContainSingle(d => d.ServiceType == typeof(ISampleService));
        services.Should().ContainSingle(d => d.ServiceType == typeof(ICommandHandler<CreateSampleCommand, Guid>) && !d.IsKeyedService);
        services.Count(d => d.ServiceType == typeof(ICommandHandler<CreateSampleCommand, Guid>)).Should().Be(4);
        services.Should().ContainSingle(d => d.ServiceType == typeof(IValidator<CreateSampleCommand>));
    }

    private static bool IsHandlerService(Type type) =>
        type.IsGenericType
        && (type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) || type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

    private ServiceCollection CreateBaseServices(bool registerUnitOfWork = true)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFakeLogging());
        services.AddScoped(_ => _probe);
        if (registerUnitOfWork)
        {
            services.AddScoped(_ => _unitOfWork);
        }

        return services;
    }

    private ServiceCollection CreateServices(params Assembly[] assemblies)
    {
        var services = CreateBaseServices();
        services.AddConventionalServices(assemblies);
        return services;
    }
}
