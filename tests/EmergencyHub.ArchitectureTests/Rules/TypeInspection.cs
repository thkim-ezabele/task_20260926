using System.Reflection;
using System.Runtime.CompilerServices;

namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>규칙 조건에 쓰는 리플렉션 판별 함수.</summary>
public static class TypeInspection
{
    private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>추상이 아닌 클래스이면 <see langword="true"/>(static 클래스는 IL에서 abstract라 제외된다).</summary>
    /// <param name="type">검사할 형식.</param>
    /// <returns>판별 결과.</returns>
    public static bool IsConcreteClass(Type type) => type.IsClass && !type.IsAbstract;

    /// <summary>
    /// 컴파일러가 만든 record이면 <see langword="true"/>. record class는 <c>&lt;Clone&gt;$</c>,
    /// record struct는 컴파일러 생성 <c>PrintMembers</c>로 판별한다(명시적 IL 표식이 없으므로 합성 멤버로 본다).
    /// </summary>
    /// <param name="type">검사할 형식.</param>
    /// <returns>판별 결과.</returns>
    public static bool IsRecord(Type type)
    {
        if (type.IsValueType)
        {
            return type.GetMethod("PrintMembers", AllInstance)?.GetCustomAttribute<CompilerGeneratedAttribute>() is not null;
        }

        return type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;
    }

    /// <summary>열린 제네릭 인터페이스(예: <c>ICommandHandler&lt;,&gt;</c>)의 닫힌 형식을 하나라도 구현하면 <see langword="true"/>.</summary>
    /// <param name="type">검사할 형식.</param>
    /// <param name="openInterface">열린 제네릭 인터페이스.</param>
    /// <returns>판별 결과.</returns>
    public static bool ImplementsOpenGeneric(Type type, Type openInterface) =>
        type.GetInterfaces().Any(implemented => implemented.IsGenericType && implemented.GetGenericTypeDefinition() == openInterface);

    /// <summary>열린 제네릭 기반 클래스(예: <c>Entity&lt;&gt;</c>)에서 파생했으면 그 닫힌 기반 형식, 아니면 <see langword="null"/>.</summary>
    /// <param name="type">검사할 형식.</param>
    /// <param name="openBase">열린 제네릭 기반 클래스.</param>
    /// <returns>가장 가까운 닫힌 기반 형식.</returns>
    public static Type? FindOpenGenericBase(Type type, Type openBase)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openBase)
            {
                return current;
            }
        }

        return null;
    }

    /// <summary>생성자(public · non-public) 매개변수 형식. primary constructor 매개변수도 포함한다.</summary>
    /// <param name="type">검사할 형식.</param>
    /// <returns>중복 없는 매개변수 형식.</returns>
    public static IReadOnlyList<Type> ConstructorDependencies(Type type) =>
        [.. type.GetConstructors(AllInstance).SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType).Distinct()];

    /// <summary>생성자 매개변수 중 주어진 형식(마커 등)에 할당 가능한 것이 있으면 <see langword="true"/>.</summary>
    /// <param name="type">검사할 형식.</param>
    /// <param name="forbidden">금지할 형식(인터페이스 또는 클래스).</param>
    /// <returns>판별 결과.</returns>
    public static bool InjectsAnyOf(Type type, params Type[] forbidden) =>
        ConstructorDependencies(type).Any(dependency => forbidden.Any(marker => marker.IsAssignableFrom(dependency)));

    /// <summary>제네릭 인자 수 표기(<c>`1</c>)를 뺀 형식 이름.</summary>
    /// <param name="type">검사할 형식.</param>
    /// <returns>형식 이름.</returns>
    public static string SimpleName(Type type)
    {
        var tick = type.Name.IndexOf('`', StringComparison.Ordinal);

        return tick < 0 ? type.Name : type.Name[..tick];
    }
}
