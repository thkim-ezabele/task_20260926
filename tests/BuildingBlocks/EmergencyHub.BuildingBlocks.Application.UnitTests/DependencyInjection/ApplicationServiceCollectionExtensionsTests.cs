using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.DependencyInjection;

// AddBuildingBlocksApplication: Application 공통 등록(ISender Scoped, TimeProvider.System Singleton). ADR-0010 · 0015 · 0017.
public sealed class ApplicationServiceCollectionExtensionsTests
{
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    // ---- 성공 ----

    [Fact]
    public void AddBuildingBlocksApplication_Called_RegistersSenderAsScopedWithSenderImplementation()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksApplication();

        var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(ISender)).Subject;
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be<Sender>();
    }

    [Fact]
    public void AddBuildingBlocksApplication_Called_RegistersTimeProviderSystemAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksApplication();

        var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(TimeProvider)).Subject;
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationInstance.Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddBuildingBlocksApplication_Called_ReturnsSameCollectionForChaining()
    {
        var services = new ServiceCollection();

        var returned = services.AddBuildingBlocksApplication();

        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void BuildServiceProvider_StrictValidation_ResolvesSenderThroughPublicConstructor()
    {
        var services = new ServiceCollection().AddBuildingBlocksApplication();

        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        // DI는 public 생성자 Sender(IServiceProvider)만 고른다. internal 생성자(테스트 전용 캐시)는 후보가 아니다.
        scope.ServiceProvider.GetRequiredService<ISender>().Should().BeOfType<Sender>();
    }

    [Fact]
    public void ResolveSender_SameScopeTwiceAndOtherScope_SameInstanceInScopeAndNewInstanceInOtherScope()
    {
        using var provider = new ServiceCollection().AddBuildingBlocksApplication().BuildServiceProvider(StrictOptions);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<ISender>();
        var b = first.ServiceProvider.GetRequiredService<ISender>();
        var c = second.ServiceProvider.GetRequiredService<ISender>();

        a.Should().BeSameAs(b);
        a.Should().NotBeSameAs(c);
    }

    [Fact]
    public void ResolveTimeProvider_FromRootAndScope_ReturnsTimeProviderSystem()
    {
        using var provider = new ServiceCollection().AddBuildingBlocksApplication().BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
        scope.ServiceProvider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }

    // ---- 실패 ----

    [Fact]
    public void AddBuildingBlocksApplication_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddBuildingBlocksApplication();

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void ResolveSender_ScopedFromRootProvider_ThrowsBecauseValidateScopes()
    {
        // Scoped 등록이 맞는지(Singleton으로 잘못 등록되지 않았는지)를 ValidateScopes 동작으로 확인한다.
        using var provider = new ServiceCollection().AddBuildingBlocksApplication().BuildServiceProvider(StrictOptions);

        var act = () => provider.GetRequiredService<ISender>();

        act.Should().Throw<InvalidOperationException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void AddBuildingBlocksApplication_CalledTwice_KeepsSingleRegistrationEach()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksApplication();
        services.AddBuildingBlocksApplication();

        services.Count(d => d.ServiceType == typeof(ISender)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(TimeProvider)).Should().Be(1);
    }

    [Fact]
    public void AddBuildingBlocksApplication_TimeProviderAlreadyRegistered_KeepsExistingRegistration()
    {
        // 테스트 호스트가 FakeTimeProvider를 먼저 등록하면 그대로 둔다(TryAdd).
        var fake = new FakeTimeProvider();
        var services = new ServiceCollection().AddSingleton<TimeProvider>(fake);

        services.AddBuildingBlocksApplication();

        using var provider = services.BuildServiceProvider(StrictOptions);
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(fake);
    }

    [Fact]
    public void AddBuildingBlocksApplication_Called_RegistersOnlySenderAndTimeProvider()
    {
        // Handler · 데코레이터 · Validator 등록은 Infrastructure의 AddConventionalServices 몫이다(Application은 Scrutor를 모른다).
        var services = new ServiceCollection().AddBuildingBlocksApplication();

        services.Select(d => d.ServiceType).Should().BeEquivalentTo([typeof(ISender), typeof(TimeProvider)]);
    }
}
