using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.DependencyInjection;
using EmergencyHub.BuildingBlocks.Application.Pipeline;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// 규칙 기반 DI 자동 등록의 진입점입니다(ADR-0010, Scrutor 구체화는 ADR-0017).
/// </summary>
public static class ConventionalServiceCollectionExtensions
{
    /// <summary>
    /// 지정한 어셈블리에서 마커 구현 타입 · Handler · Validator를 찾아 Scoped로 등록하고, Handler를 파이프라인 데코레이터로 감쌉니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="assemblies">검색할 어셈블리(서비스의 Application · Infrastructure 등). 같은 어셈블리가 여러 번 있어도 한 번만 검색합니다.</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 또는 <paramref name="assemblies"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException">어셈블리가 없거나 <see langword="null"/> 항목이 있는 경우.</exception>
    /// <exception cref="InvalidOperationException">
    /// 이미 호출된 경우(두 번 부르면 데코레이터가 두 겹이 되어 커밋 · 로그가 중복됨), 또는 마커 구현 클래스가 서비스 인터페이스를 정확히 하나 구현하지 않는 경우.
    /// </exception>
    /// <exception cref="DuplicateTypeRegistrationException">같은 서비스 인터페이스 · 같은 Handler 인터페이스를 두 구현이 등록하려는 경우(이미 등록된 것 포함).</exception>
    /// <remarks>
    /// <list type="number">
    /// <item><description>
    /// 마커(<c>IRepository</c> · <c>IReadRepository</c> · <c>IService</c>) 구현: Scrutor <c>Scan</c>(internal 포함, 추상 제외) → 마커를 상속한 서비스 인터페이스(마커 제외)로 Scoped,
    /// <see cref="RegistrationStrategy.Throw"/>.
    /// </description></item>
    /// <item><description>
    /// Handler: <c>ICommandHandler&lt;,&gt;</c> · <c>IQueryHandler&lt;,&gt;</c> 두 인자 닫힌 형식으로만 Scoped, <see cref="RegistrationStrategy.Throw"/>.
    /// open generic 정의(데코레이터 등)는 Handler로 보지 않습니다.
    /// </description></item>
    /// <item><description>
    /// 데코레이터: <see cref="PipelineDecorators"/> 목록(안쪽 → 바깥) 순서로 <c>TryDecorate</c>. 해석 결과는 Command가 로깅 → 검증 → 트랜잭션 → Handler,
    /// Query가 로깅 → 검증 → Handler입니다. Handler가 없어도 시작은 실패하지 않습니다.
    /// </description></item>
    /// <item><description>Validator: FluentValidation <c>AddValidatorsFromAssemblies</c>(Scoped, internal 포함, ADR-0018).</description></item>
    /// </list>
    /// 데코레이터 의존(<c>ISender</c> · <c>TimeProvider</c>)이 빠지지 않도록 <see cref="ApplicationServiceCollectionExtensions.AddBuildingBlocksApplication"/>도 부릅니다(TryAdd).
    /// 트랜잭션 데코레이터가 쓰는 <c>IUnitOfWork</c>는 <see cref="PersistenceServiceCollectionExtensions.AddUnitOfWork{TContext}"/>가, <c>ILogger&lt;T&gt;</c>는 호스트가 등록합니다.
    /// </remarks>
    public static IServiceCollection AddConventionalServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);
        if (assemblies.Length == 0)
        {
            throw new ArgumentException("검색할 어셈블리를 하나 이상 지정해야 합니다.", nameof(assemblies));
        }

        if (assemblies.Any(assembly => assembly is null))
        {
            throw new ArgumentException("검색할 어셈블리에 null을 넣을 수 없습니다.", nameof(assemblies));
        }

        if (services.Any(descriptor => descriptor.ServiceType == typeof(ConventionalServicesRegistration)))
        {
            throw new InvalidOperationException(
                "AddConventionalServices는 한 번만 호출합니다. 검색할 어셈블리를 한 번에 모두 넘기세요(두 번 부르면 Handler 데코레이터가 두 겹이 됩니다).");
        }

        Assembly[] targets = [.. assemblies.Distinct()];
        ConventionalServiceTypes.EnsureSingleServiceInterface(targets);

        services.AddBuildingBlocksApplication();
        services.AddSingleton(ConventionalServicesRegistration.Instance);

        services.Scan(scan => scan
            .FromAssemblies(targets)
            .AddClasses(classes => classes.AssignableToAny(ConventionalServiceTypes.Markers), publicOnly: false)
                .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                .As(ConventionalServiceTypes.GetServiceInterfaces)
                .WithScopedLifetime()
            .AddClasses(
                classes => classes
                    .AssignableToAny(ConventionalServiceTypes.HandlerInterfaces)
                    .Where(type => !type.IsGenericTypeDefinition),
                publicOnly: false)
                .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                .As(ConventionalServiceTypes.GetHandlerInterfaces)
                .WithScopedLifetime());

        foreach (var decorator in PipelineDecorators.CommandHandlerDecorators)
        {
            services.TryDecorate(typeof(ICommandHandler<,>), decorator);
        }

        foreach (var decorator in PipelineDecorators.QueryHandlerDecorators)
        {
            services.TryDecorate(typeof(IQueryHandler<,>), decorator);
        }

        services.AddValidatorsFromAssemblies(targets, ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }
}
