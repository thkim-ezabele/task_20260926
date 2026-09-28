using System.Reflection;
using System.Reflection.Emit;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>
/// 규칙을 어기는 구현(같은 인터페이스 두 구현, 서비스 인터페이스 둘, 마커 직접 구현)을 담은 별도 어셈블리를 실행 중에 만듭니다.
/// </summary>
/// <remarks>
/// 이 테스트 어셈블리 자체도 검색 대상이라 위반 예시를 여기 두면 정상 시나리오가 모두 실패한다. 그래서 위반 예시는 호출마다 새 동적 어셈블리에 만든다.
/// 만든 클래스는 public sealed, 기본 생성자, 인터페이스 메서드는 모두 <see cref="NotSupportedException"/>을 던진다(등록만 확인하므로 호출하지 않음).
/// </remarks>
public static class DynamicSampleAssembly
{
    /// <summary>주어진 형식들을 담은 동적 어셈블리를 만듭니다.</summary>
    /// <param name="types">만들 클래스 이름과 구현할 (public) 인터페이스.</param>
    /// <returns>만든 어셈블리.</returns>
    public static Assembly Define(params (string Name, Type[] Interfaces)[] types)
    {
        ArgumentNullException.ThrowIfNull(types);

        var assemblyName = new AssemblyName($"DynamicSamples{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(assemblyName.Name!);

        foreach (var (name, interfaces) in types)
        {
            var type = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(object), interfaces);
            type.DefineDefaultConstructor(MethodAttributes.Public);

            var allInterfaces = interfaces.SelectMany(i => i.GetInterfaces().Prepend(i)).Distinct();
            foreach (var method in allInterfaces.SelectMany(i => i.GetMethods()))
            {
                var implementation = type.DefineMethod(
                    $"{method.DeclaringType!.FullName}.{method.Name}",
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    method.ReturnType,
                    [.. method.GetParameters().Select(p => p.ParameterType)]);
                var il = implementation.GetILGenerator();
                il.Emit(OpCodes.Newobj, typeof(NotSupportedException).GetConstructor(Type.EmptyTypes)!);
                il.Emit(OpCodes.Throw);
                type.DefineMethodOverride(implementation, method);
            }

            type.CreateType();
        }

        return assembly;
    }
}
