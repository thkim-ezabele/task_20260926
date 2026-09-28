using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>
/// 해석된 Handler에서 데코레이터 체인을 바깥 → 안쪽으로 펼칩니다.
/// </summary>
/// <remarks>
/// 데코레이터는 internal이라 형식을 직접 참조할 수 없으므로, 각 단계에서 같은 Handler 인터페이스 형식의 필드(primary constructor가 캡처한 inner)를 찾아 따라간다.
/// 제네릭 형식은 open generic 정의로 바꿔 <c>PipelineDecorators</c> 목록과 비교할 수 있게 한다.
/// </remarks>
public static class HandlerChain
{
    /// <summary>체인을 바깥 → 안쪽 순서의 형식 목록으로 돌려줍니다(마지막이 실제 Handler).</summary>
    /// <param name="handler">해석된 가장 바깥 Handler.</param>
    /// <returns>각 단계의 형식(제네릭이면 open generic 정의).</returns>
    public static IReadOnlyList<Type> Unwrap(object handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var chain = new List<Type>();
        object? current = handler;
        while (current is not null)
        {
            var type = current.GetType();
            chain.Add(type.IsGenericType ? type.GetGenericTypeDefinition() : type);
            current = FindInner(current, type);
        }

        return chain;
    }

    private static object? FindInner(object instance, Type type)
    {
        var inner = type
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .SingleOrDefault(field => IsHandlerInterface(field.FieldType));
        return inner?.GetValue(instance);
    }

    private static bool IsHandlerInterface(Type type) =>
        type.IsInterface
        && type.IsGenericType
        && (type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) || type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
}
