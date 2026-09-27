using EmergencyHub.BuildingBlocks.Application.DependencyInjection;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Infrastructure.Identifiers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// BuildingBlocks.Infrastructure의 공통 DI 등록입니다(ADR-0017 "공통 인프라 등록").
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// <see cref="IIdGenerator"/>(UUID v7, Scoped)와 Application 공통 등록(<c>ISender</c>, <c>TimeProvider</c>)을 합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/>가 <see langword="null"/>인 경우.</exception>
    /// <remarks>
    /// <para>
    /// <see cref="IIdGenerator"/>는 <c>IService</c>를 상속하지 않고 여기서 <b>명시 등록</b>합니다. ADR-0017이 <c>IIdGenerator</c>를
    /// "BuildingBlocks 공통 등록 코드에서 명시 등록"하는 공통 인프라로 정했고, 구현이 BuildingBlocks.Infrastructure에 있어
    /// 서비스 어셈블리 검색(<see cref="ConventionalServiceCollectionExtensions.AddConventionalServices"/>)으로는 찾을 수 없기 때문입니다.
    /// 수명은 ADR-0013대로 Scoped입니다. 서비스 초기화 코드가 구현을 개별 등록하지 않는다는 ADR-0010의 취지는 그대로 지켜집니다.
    /// </para>
    /// <para>모두 <c>TryAdd</c>라 여러 번 불러도 등록은 하나이고, 테스트 호스트가 먼저 등록한 대역은 그대로 둡니다.</para>
    /// </remarks>
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBuildingBlocksApplication();
        services.TryAddScoped<IIdGenerator, UuidV7IdGenerator>();

        return services;
    }
}
