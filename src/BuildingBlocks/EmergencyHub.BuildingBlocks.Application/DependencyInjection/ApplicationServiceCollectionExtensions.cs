using EmergencyHub.BuildingBlocks.Application.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmergencyHub.BuildingBlocks.Application.DependencyInjection;

/// <summary>
/// BuildingBlocks.Application의 공통 DI 등록입니다(ADR-0010의 Singleton / Transient 예외 규칙, ADR-0017 "공통 인프라 등록").
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// 디스패처 <see cref="ISender"/>(Scoped)와 <see cref="TimeProvider.System"/>(Singleton)을 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/>가 <see langword="null"/>인 경우.</exception>
    /// <remarks>
    /// <para>
    /// 둘 다 <c>TryAdd</c>로 등록합니다. 여러 번 불러도 등록은 하나이고, 테스트 호스트가 먼저 등록한
    /// <see cref="TimeProvider"/>(예: <c>FakeTimeProvider</c>)는 그대로 둡니다.
    /// </para>
    /// <para>
    /// Handler · Validator · 데코레이터는 여기서 등록하지 않습니다. Scrutor를 쓰는 Infrastructure의 <c>AddConventionalServices</c>가
    /// <see cref="Pipeline.PipelineDecorators"/> 순서로 등록합니다(Application은 Scrutor를 참조하지 않음, ADR-0017).
    /// </para>
    /// </remarks>
    public static IServiceCollection AddBuildingBlocksApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ISender, Sender>();

        return services;
    }
}
