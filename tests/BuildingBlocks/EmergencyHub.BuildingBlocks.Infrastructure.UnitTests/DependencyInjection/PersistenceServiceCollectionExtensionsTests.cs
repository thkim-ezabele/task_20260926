using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Auditing;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.DependencyInjection;

// database.md "EF Core 구성 · 공통 DbContext 등록"(S02-T07): 옵션 구성 한 메서드(UseNpgsql + EnableRetryOnFailure + snake_case),
// 쓰기만 감사 인터셉터(Singleton), 쓰기 · 읽기 등록 분리, 연결 문자열 없으면 시작 시 예외, AddUnitOfWork는 IUnitOfWork를 쓰기 DbContext에 묶어 Scoped.
// 연결을 열지 않는다(더미 연결 문자열). 실제 DB 동작은 S03-T05.
[Trait("FR", "PRD-001/FR-06")]
public sealed class PersistenceServiceCollectionExtensionsTests
{
    private const string Connection = SampleDbContexts.DummyConnectionString;

    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    // ---- 쓰기 DbContext ----

    [Fact]
    public void AddWriteDbContext_Called_RegistersContextAsScoped()
    {
        var services = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection);

        services.Should().ContainSingle(d => d.ServiceType == typeof(SampleWriteDbContext))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddWriteDbContext_Resolved_UsesNpgsqlWithRetryingStrategySnakeCaseAndAuditInterceptor()
    {
        using var provider = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>();

        context.Database.IsNpgsql().Should().BeTrue();
        context.Database.CreateExecutionStrategy().RetriesOnFailure.Should().BeTrue();
        context.Model.FindEntityType(typeof(Order))!.GetTableName().Should().Be("orders");
        Interceptors(context).Should().ContainSingle(interceptor => interceptor is AuditSaveChangesInterceptor);
    }

    [Fact]
    public void AddWriteDbContext_Resolved_HasSameCreateScriptAsSharedOptionsPath()
    {
        // 등록 확장 · 샘플(메타데이터 테스트) · 설계 시점 팩터리가 같은 옵션 구성 메서드를 쓰므로 모델이 같다.
        using var provider = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();
        using var direct = SampleDbContexts.CreateWrite();

        var registered = scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>();

        registered.Database.GenerateCreateScript().Should().Be(direct.Database.GenerateCreateScript());
    }

    [Fact]
    public void AddWriteDbContext_Resolved_ConnectionStringIsPassedToNpgsql()
    {
        using var provider = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>();

        context.Database.GetConnectionString().Should().Be(Connection);
    }

    [Fact]
    public void AddWriteDbContext_AuditInterceptor_IsOneSingletonSharedAcrossScopes()
    {
        var services = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection);
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var firstInterceptor = Interceptors(first.ServiceProvider.GetRequiredService<SampleWriteDbContext>()).OfType<AuditSaveChangesInterceptor>().Single();
        var secondInterceptor = Interceptors(second.ServiceProvider.GetRequiredService<SampleWriteDbContext>()).OfType<AuditSaveChangesInterceptor>().Single();

        services.Should().ContainSingle(d => d.ServiceType == typeof(AuditSaveChangesInterceptor))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
        firstInterceptor.Should().BeSameAs(secondInterceptor);
    }

    [Fact]
    public void AddWriteDbContext_TimeProviderAlreadyRegistered_KeepsHostTimeProvider()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero));
        var services = new ServiceCollection().AddSingleton<TimeProvider>(time).AddWriteDbContext<SampleWriteDbContext>(Connection);
        using var provider = services.BuildServiceProvider(StrictOptions);

        services.Should().ContainSingle(d => d.ServiceType == typeof(TimeProvider));
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(time);
    }

    [Fact]
    public void AddWriteDbContext_WithoutTimeProvider_RegistersSystemTimeProvider()
    {
        var services = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection);

        services.Should().ContainSingle(d => d.ServiceType == typeof(TimeProvider))
            .Which.ImplementationInstance.Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddWriteDbContext_WithConfigure_AppliesExtraOptionsAfterCommonOptions()
    {
        var database = new FakeDatabase();
        using var provider = new ServiceCollection()
            .AddWriteDbContext<SampleWriteDbContext>(Connection, options => options.AddInterceptors(database))
            .BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var interceptors = Interceptors(scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>());

        interceptors.Should().Contain(database);
        interceptors.Should().ContainSingle(interceptor => interceptor is AuditSaveChangesInterceptor);
    }

    [Fact]
    public void AddWriteDbContext_OnlyWriteRegistered_BuildsWithoutReadConnection()
    {
        // MigrationService는 쓰기만 등록한다(ConnectionStrings:Read가 없어도 시작해야 함).
        using var provider = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetService<SampleReadDbContext>().Should().BeNull();
        scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>().Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddWriteDbContext_MissingConnectionString_ThrowsAtRegistrationNamingTheKey(string? connectionString)
    {
        var services = new ServiceCollection();

        var act = () => services.AddWriteDbContext<SampleWriteDbContext>(connectionString);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:Write*");
        services.Should().BeEmpty("실패하면 등록을 남기지 않는다");
    }

    [Fact]
    public void AddWriteDbContext_NullServices_ThrowsArgumentNullException()
    {
        var act = () => PersistenceServiceCollectionExtensions.AddWriteDbContext<SampleWriteDbContext>(null!, Connection);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddWriteDbContext_WithRetryOptions_PassesThemToExecutionStrategy()
    {
        using var provider = new ServiceCollection()
            .AddWriteDbContext<SampleWriteDbContext>(Connection, retry: new DbRetryOptions(3, TimeSpan.FromSeconds(5)))
            .BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var strategy = scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>().Database.CreateExecutionStrategy();

        ExecutionStrategySettings.MaxRetryCount(strategy).Should().Be(3);
        ExecutionStrategySettings.MaxRetryDelay(strategy).Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddWriteDbContext_WithRetryOptionsAndConfigure_KeepsRetrySettingsAndAuditInterceptor()
    {
        // 엣지: 추가 옵션 콜백이 있어도 재시도 설정은 공통 옵션 구성 한 곳에서 정해진다(콜백은 UseNpgsql을 다시 부르지 않음).
        var database = new FakeDatabase();
        using var provider = new ServiceCollection()
            .AddWriteDbContext<SampleWriteDbContext>(Connection, options => options.AddInterceptors(database), new DbRetryOptions(1, TimeSpan.FromSeconds(2)))
            .BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>();

        ExecutionStrategySettings.MaxRetryCount(context.Database.CreateExecutionStrategy()).Should().Be(1);
        Interceptors(context).Should().Contain(database).And.ContainSingle(interceptor => interceptor is AuditSaveChangesInterceptor);
    }

    [Fact]
    public void AddWriteDbContext_WithoutRetryOptions_UsesNpgsqlDefaultRetrySettings()
    {
        using var provider = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var strategy = scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>().Database.CreateExecutionStrategy();

        ExecutionStrategySettings.MaxRetryCount(strategy).Should().Be(6);
        ExecutionStrategySettings.MaxRetryDelay(strategy).Should().Be(TimeSpan.FromSeconds(30));
    }

    // ---- 읽기 DbContext ----

    [Fact]
    public void AddReadDbContext_Resolved_IsScopedNoTrackingSnakeCaseRetryingWithoutAuditInterceptor()
    {
        var services = new ServiceCollection().AddReadDbContext<SampleReadDbContext>(Connection);
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<SampleReadDbContext>();

        services.Should().ContainSingle(d => d.ServiceType == typeof(SampleReadDbContext)).Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        context.Model.FindEntityType(typeof(Order))!.GetTableName().Should().Be("orders");
        context.Database.CreateExecutionStrategy().RetriesOnFailure.Should().BeTrue();
        Interceptors(context).Should().NotContain(interceptor => interceptor is AuditSaveChangesInterceptor);
        services.Should().NotContain(d => d.ServiceType == typeof(AuditSaveChangesInterceptor));
    }

    [Fact]
    public void AddReadDbContext_Resolved_HasSameCreateScriptAsWriteRegistration()
    {
        using var provider = new ServiceCollection()
            .AddWriteDbContext<SampleWriteDbContext>(Connection)
            .AddReadDbContext<SampleReadDbContext>(Connection)
            .BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var read = scope.ServiceProvider.GetRequiredService<SampleReadDbContext>();
        var write = scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>();

        read.Database.GenerateCreateScript().Should().Be(write.Database.GenerateCreateScript());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddReadDbContext_MissingConnectionString_ThrowsAtRegistrationNamingTheKey(string? connectionString)
    {
        var act = () => new ServiceCollection().AddReadDbContext<SampleReadDbContext>(connectionString);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:Read*");
    }

    [Fact]
    public void AddReadDbContext_WithRetryOptions_PassesThemToExecutionStrategy()
    {
        using var provider = new ServiceCollection()
            .AddReadDbContext<SampleReadDbContext>(Connection, retry: new DbRetryOptions(2, TimeSpan.FromSeconds(3)))
            .BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var strategy = scope.ServiceProvider.GetRequiredService<SampleReadDbContext>().Database.CreateExecutionStrategy();

        ExecutionStrategySettings.MaxRetryCount(strategy).Should().Be(2);
        ExecutionStrategySettings.MaxRetryDelay(strategy).Should().Be(TimeSpan.FromSeconds(3));
    }

    // ---- UnitOfWork ----

    [Fact]
    public void AddUnitOfWork_Called_RegistersScopedUnitOfWorkBoundToWriteContextAndSingletonRegistry()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddWriteDbContext<SampleWriteDbContext>(Connection)
            .AddUnitOfWork<SampleWriteDbContext>(SampleUniqueConstraintErrors.Configure);

        services.Should().ContainSingle(d => d.ServiceType == typeof(IUnitOfWork))
            .Which.Should().Match<ServiceDescriptor>(d =>
                d.Lifetime == ServiceLifetime.Scoped && d.ImplementationType == typeof(UnitOfWork<SampleWriteDbContext>));
        services.Should().ContainSingle(d => d.ServiceType == typeof(UniqueConstraintErrorRegistry))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddUnitOfWork_WithMapping_RegistryFromProviderContainsMappedIndex()
    {
        using var provider = CreateUnitOfWorkServices(SampleUniqueConstraintErrors.Configure).BuildServiceProvider(StrictOptions);

        var registry = provider.GetRequiredService<UniqueConstraintErrorRegistry>();

        registry.IndexNames.Should().Equal(SampleIndexNames.OrdersOrderNumber);
        registry.Find(SampleIndexNames.OrdersOrderNumber.Value).Should().Be(SampleErrors.ValueConflict);
    }

    [Fact]
    public void AddUnitOfWork_WithoutMapping_RegistersEmptyRegistry()
    {
        using var provider = CreateUnitOfWorkServices(configure: null).BuildServiceProvider(StrictOptions);

        provider.GetRequiredService<UniqueConstraintErrorRegistry>().IndexNames.Should().BeEmpty();
    }

    [Fact]
    public async Task AddUnitOfWork_ResolvedInScope_UsesSameContextInstanceAsWriteRepository()
    {
        // UoW와 Write Repository는 같은 스코프의 같은 DbContext 인스턴스를 쓴다: Repository가 추가한 엔트리를 UoW가 저장 · 승인한다.
        var database = new FakeDatabase();
        var services = new ServiceCollection()
            .AddLogging()
            .AddWriteDbContext<SampleWriteDbContext>(Connection, options => options.AddInterceptors(database))
            .AddUnitOfWork<SampleWriteDbContext>();
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();
        var repository = new SampleOrderRepository(scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>());
        var order = SampleDbContexts.NewOrder();
        repository.Add(order);

        var result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        database.Steps.Should().Contain("Commit");
        repository.ExposedDb.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>());
        repository.ExposedDb.Entry(order).State.Should().Be(EntityState.Unchanged);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddUnitOfWork_TwoScopes_GetDifferentUnitOfWorkAndContextInstances()
    {
        using var provider = CreateUnitOfWorkServices(configure: null).BuildServiceProvider(StrictOptions);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        first.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeSameAs(second.ServiceProvider.GetRequiredService<IUnitOfWork>());
        first.ServiceProvider.GetRequiredService<SampleWriteDbContext>()
            .Should().NotBeSameAs(second.ServiceProvider.GetRequiredService<SampleWriteDbContext>());
    }

    [Fact]
    public void AddUnitOfWork_CalledAgainWithOtherContext_ThrowsInvalidOperationException()
    {
        var services = CreateUnitOfWorkServices(configure: null).AddWriteDbContext<DispatchWriteDbContext>(Connection);

        var act = () => services.AddUnitOfWork<DispatchWriteDbContext>();

        act.Should().Throw<InvalidOperationException>().WithMessage("*AddUnitOfWork*");
    }

    [Fact]
    public void AddUnitOfWork_CalledAgainWithSameContext_ThrowsInvalidOperationException()
    {
        var services = CreateUnitOfWorkServices(configure: null);

        var act = () => services.AddUnitOfWork<SampleWriteDbContext>();

        act.Should().Throw<InvalidOperationException>();
        services.Count(d => d.ServiceType == typeof(IUnitOfWork)).Should().Be(1);
    }

    [Fact]
    public void AddUnitOfWork_DuplicateIndexInConfigure_ThrowsAtRegistration()
    {
        var services = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection);

        var act = () => services.AddUnitOfWork<SampleWriteDbContext>(errors => errors
            .Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueConflict)
            .Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueConflict));

        act.Should().Throw<InvalidOperationException>();
        services.Should().NotContain(d => d.ServiceType == typeof(IUnitOfWork));
    }

    [Fact]
    public void AddUnitOfWork_NonConflictErrorInConfigure_ThrowsArgumentExceptionAtRegistration()
    {
        var services = new ServiceCollection().AddWriteDbContext<SampleWriteDbContext>(Connection);

        var act = () => services.AddUnitOfWork<SampleWriteDbContext>(errors => errors.Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueNegative));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddUnitOfWork_WriteContextNotRegistered_FailsAtBuildBecauseOfValidateOnBuild()
    {
        var services = new ServiceCollection().AddLogging().AddUnitOfWork<SampleWriteDbContext>();

        var act = () => services.BuildServiceProvider(StrictOptions);

        act.Should().Throw<AggregateException>().WithMessage($"*{nameof(SampleWriteDbContext)}*");
    }

    [Fact]
    public async Task AddUnitOfWork_PreCommitHooksRegistered_AreInjectedIntoUnitOfWork()
    {
        var database = new FakeDatabase();
        var hook = new RecordingPreCommitHook(database);
        var services = new ServiceCollection()
            .AddLogging()
            .AddScoped<IPreCommitHook>(_ => hook)
            .AddWriteDbContext<SampleWriteDbContext>(Connection, options => options.AddInterceptors(database))
            .AddUnitOfWork<SampleWriteDbContext>();
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        hook.Contexts.Should().ContainSingle().Which.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<SampleWriteDbContext>());
    }

    // ---- 예외 분류기 ----

    [Fact]
    public void AddBuildingBlocksInfrastructure_Called_RegistersPersistenceExceptionClassifierOnceAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksInfrastructure();
        services.AddBuildingBlocksInfrastructure();

        services.Should().ContainSingle(d => d.ServiceType == typeof(IExceptionClassifier))
            .Which.Should().Match<ServiceDescriptor>(d => d.Lifetime == ServiceLifetime.Singleton);
        using var provider = services.BuildServiceProvider(StrictOptions);
        provider.GetServices<IExceptionClassifier>().Should().ContainSingle().Which.Should().BeOfType<PersistenceExceptionClassifier>();
    }

    [Fact]
    public void AddBuildingBlocksInfrastructure_OtherClassifierRegistered_KeepsBothClassifiers()
    {
        var services = new ServiceCollection().AddSingleton<IExceptionClassifier, SampleExceptionClassifier>();

        services.AddBuildingBlocksInfrastructure();

        using var provider = services.BuildServiceProvider(StrictOptions);
        provider.GetServices<IExceptionClassifier>().Select(classifier => classifier.GetType())
            .Should().BeEquivalentTo([typeof(SampleExceptionClassifier), typeof(PersistenceExceptionClassifier)]);
    }

    private static ServiceCollection CreateUnitOfWorkServices(Action<UniqueConstraintErrorsBuilder>? configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWriteDbContext<SampleWriteDbContext>(Connection);
        services.AddUnitOfWork<SampleWriteDbContext>(configure);
        return services;
    }

    private static IReadOnlyList<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor> Interceptors(DbContext context) =>
        [.. context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()!.Interceptors ?? []];
}
