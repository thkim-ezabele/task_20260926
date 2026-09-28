using Mono.Cecil;

namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 리플렉션 조건을 NetArchTest 사용자 규칙(<see cref="ICustomRule"/>)으로 쓰게 한다.
/// </summary>
/// <remarks>
/// NetArchTest는 Mono.Cecil 형식 정의로 검사하므로 열린 제네릭 인터페이스 구현 · 생성자 매개변수 · record 여부처럼
/// 이름 기반 술어로 표현하기 어려운 조건은 이미 로드된 제품 어셈블리의 <see cref="Type"/>으로 바꿔 판단한다.
/// </remarks>
/// <param name="predicate">형식이 조건을 만족하면 <see langword="true"/>.</param>
public sealed class TypeRule(Func<Type, bool> predicate) : ICustomRule
{
    /// <inheritdoc />
    public bool MeetsRule(TypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return predicate(Resolve(type));
    }

    private static Type Resolve(TypeDefinition definition)
    {
        var assemblyName = definition.Module.Assembly.Name.Name;
        var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(loaded => loaded.GetName().Name == assemblyName);

        return assembly.GetType(definition.FullName.Replace('/', '+'), throwOnError: true)!;
    }
}
