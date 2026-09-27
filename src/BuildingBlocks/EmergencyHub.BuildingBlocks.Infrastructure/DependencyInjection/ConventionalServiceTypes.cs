using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Services;
using EmergencyHub.BuildingBlocks.Domain.Repositories;

namespace EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// 규칙 기반 등록(ADR-0010, ADR-0017)에서 쓰는 형식 판별 규칙입니다.
/// </summary>
internal static class ConventionalServiceTypes
{
    /// <summary>자동 등록 마커. 구현 클래스는 이 마커를 상속한 서비스 인터페이스로 등록됩니다.</summary>
    public static readonly Type[] Markers = [typeof(IRepository), typeof(IReadRepository), typeof(IService)];

    /// <summary>Handler 등록 형태. 두 제네릭 인자 형태만 씁니다(한 인자 편의 인터페이스는 데코레이터 우회 경로가 되므로 제외).</summary>
    public static readonly Type[] HandlerInterfaces = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    /// <summary>구현 형식이 구현한 서비스 인터페이스(마커를 상속한 인터페이스, 마커 자신 제외)를 돌려줍니다.</summary>
    /// <param name="implementationType">구현 형식.</param>
    /// <returns>서비스 인터페이스 목록.</returns>
    public static IEnumerable<Type> GetServiceInterfaces(Type implementationType) =>
        implementationType.GetInterfaces().Where(IsServiceInterface);

    /// <summary>구현 형식이 구현한 두 인자 Handler 인터페이스(닫힌 형식)를 돌려줍니다.</summary>
    /// <param name="implementationType">구현 형식.</param>
    /// <returns>Handler 인터페이스 목록.</returns>
    public static IEnumerable<Type> GetHandlerInterfaces(Type implementationType) =>
        implementationType.GetInterfaces().Where(IsHandlerInterface);

    /// <summary>
    /// 마커를 구현한 클래스가 서비스 인터페이스를 정확히 하나 구현하는지 확인합니다(ADR-0010 "서비스 인터페이스 하나만 구현").
    /// </summary>
    /// <param name="assemblies">검색 대상 어셈블리.</param>
    /// <exception cref="InvalidOperationException">서비스 인터페이스가 없거나 둘 이상인 구현이 있는 경우(위반 전체를 한 메시지로).</exception>
    /// <remarks>
    /// 인터페이스가 없으면 등록이 조용히 빠지고, 둘 이상이면 등록이 모호해지므로 시작 시점에 실패시킵니다.
    /// 등록 전에 검사하므로 위반이 있으면 서비스 컬렉션을 바꾸지 않습니다.
    /// </remarks>
    public static void EnsureSingleServiceInterface(IEnumerable<Assembly> assemblies)
    {
        var violations = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type.IsClass && !type.IsAbstract && Markers.Any(marker => marker.IsAssignableFrom(type)))
            .Select(type => (Type: type, Interfaces: GetServiceInterfaces(type).ToList()))
            .Where(candidate => candidate.Interfaces.Count != 1)
            .Select(candidate => $"{candidate.Type.FullName}: [{string.Join(", ", candidate.Interfaces.Select(i => i.FullName))}]")
            .ToList();

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "마커(IRepository · IReadRepository · IService)를 구현한 클래스는 마커를 상속한 서비스 인터페이스를 정확히 하나 구현해야 합니다(ADR-0010). "
                + $"위반: {string.Join("; ", violations)}");
        }
    }

    private static bool IsServiceInterface(Type type) =>
        !Markers.Contains(type) && Markers.Any(marker => marker.IsAssignableFrom(type));

    private static bool IsHandlerInterface(Type type) =>
        type.IsGenericType && HandlerInterfaces.Contains(type.GetGenericTypeDefinition());
}
