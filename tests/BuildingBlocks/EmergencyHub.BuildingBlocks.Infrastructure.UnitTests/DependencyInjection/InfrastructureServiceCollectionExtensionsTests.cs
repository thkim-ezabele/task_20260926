using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using EmergencyHub.BuildingBlocks.Infrastructure.Identifiers;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.DependencyInjection;

// AddBuildingBlocksInfrastructure: IIdGenerator 명시 등록(Scoped).
// IIdGenerator는 IService를 상속하지 않는다. ADR-0017 "공통 인프라 등록(IIdGenerator 등)은 BuildingBlocks 공통 등록 코드에서 명시 등록"을 따르고,
// 수명은 ADR-0013대로 Scoped다(생성기 상태가 UUIDNext 정적 생성기에 있어 Scoped여도 단조성 유지).
public sealed class InfrastructureServiceCollectionExtensionsTests
{
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    // ---- 성공 ----

    [Fact]
    public void AddBuildingBlocksInfrastructure_Called_RegistersIdGeneratorAsScopedUuidV7Generator()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksInfrastructure();

        var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(IIdGenerator)).Subject;
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be<UuidV7IdGenerator>();
    }

    [Fact]
    public void AddBuildingBlocksInfrastructure_Called_AlsoRegistersApplicationCommonServices()
    {
        // 데코레이터 · Handler가 쓰는 TimeProvider · ISender가 빠지지 않도록 Application 공통 등록을 함께 부른다(TryAdd라 중복 없음).
        var services = new ServiceCollection();

        services.AddBuildingBlocksInfrastructure();

        services.Should().ContainSingle(d => d.ServiceType == typeof(ISender));
        services.Should().ContainSingle(d => d.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void BuildServiceProvider_StrictValidation_ResolvesIdGeneratorInScope()
    {
        using var provider = new ServiceCollection().AddBuildingBlocksInfrastructure().BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var generator = scope.ServiceProvider.GetRequiredService<IIdGenerator>();

        generator.Should().BeOfType<UuidV7IdGenerator>();
        generator.NewId().ToString("D")[14].Should().Be('7');
    }

    [Fact]
    public void AddBuildingBlocksInfrastructure_Called_ReturnsSameCollectionForChaining()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksInfrastructure().Should().BeSameAs(services);
    }

    // ---- 실패 ----

    [Fact]
    public void AddBuildingBlocksInfrastructure_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddBuildingBlocksInfrastructure();

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void ResolveIdGenerator_FromRootProvider_ThrowsBecauseScoped()
    {
        using var provider = new ServiceCollection().AddBuildingBlocksInfrastructure().BuildServiceProvider(StrictOptions);

        var act = () => provider.GetRequiredService<IIdGenerator>();

        act.Should().Throw<InvalidOperationException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void AddBuildingBlocksInfrastructure_CalledTwice_KeepsSingleIdGeneratorRegistration()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksInfrastructure();
        services.AddBuildingBlocksInfrastructure();

        services.Count(d => d.ServiceType == typeof(IIdGenerator)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(ISender)).Should().Be(1);
    }

    [Fact]
    public void AddBuildingBlocksInfrastructure_IdGeneratorAlreadyRegistered_KeepsExistingRegistration()
    {
        // 테스트 호스트가 결정적 대역을 먼저 등록하면 그대로 둔다(ADR-0013 "단위 테스트에서는 대역").
        var services = new ServiceCollection().AddScoped<IIdGenerator, SampleIdGenerator>();

        services.AddBuildingBlocksInfrastructure();

        services.Should().ContainSingle(d => d.ServiceType == typeof(IIdGenerator))
            .Which.ImplementationType.Should().Be<SampleIdGenerator>();
    }

    [Fact]
    public void ResolveIdGenerator_TwoScopes_DifferentInstancesButContinuousOrder()
    {
        using var provider = new ServiceCollection().AddBuildingBlocksInfrastructure().BuildServiceProvider(StrictOptions);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<IIdGenerator>();
        var b = second.ServiceProvider.GetRequiredService<IIdGenerator>();
        var ids = Enumerable.Range(0, 1_000).Select(i => (i % 2 == 0 ? a : b).NewId().ToString("D")).ToList();

        a.Should().NotBeSameAs(b);
        ids.Zip(ids.Skip(1)).Should().OnlyContain(pair => string.CompareOrdinal(pair.First, pair.Second) < 0);
    }
}
