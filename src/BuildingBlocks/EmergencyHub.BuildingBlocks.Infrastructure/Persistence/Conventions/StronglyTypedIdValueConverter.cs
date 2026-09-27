using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using EmergencyHub.BuildingBlocks.Domain.Identifiers;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;

/// <summary>
/// 강타입 ID ↔ <see cref="Guid"/>(<c>uuid</c>) 값 변환기입니다(TD-015).
/// </summary>
/// <typeparam name="TId">강타입 ID 형식.</typeparam>
/// <remarks>
/// 식 트리에서는 static abstract 인터페이스 멤버를 호출할 수 없으므로(CS8927) 구체 형식의 <see cref="Guid"/> 생성자로 식을 만든다.
/// 생성자가 없으면 모델 생성 시점(이 형식을 쓰는 속성이 있을 때)에 실패한다.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "CommonModelConventions.ConfigureConventions가 형식으로 HaveConversion에 넘기고 EF Core가 매개변수 없는 생성자로 만든다.")]
internal sealed class StronglyTypedIdValueConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct, IStronglyTypedId<TId>
{
    public StronglyTypedIdValueConverter()
        : base(id => id.Value, CreateFromGuid())
    {
    }

    private static Expression<Func<Guid, TId>> CreateFromGuid()
    {
        var constructor = typeof(TId).GetConstructor([typeof(Guid)])
            ?? throw new InvalidOperationException(
                $"강타입 ID {typeof(TId).FullName}에 Guid 하나를 받는 public 생성자가 없습니다. "
                + "위치 기반 record struct(예: public readonly record struct EmployeeId(Guid Value))로 만드세요(TD-015).");
        var value = Expression.Parameter(typeof(Guid), "value");

        return Expression.Lambda<Func<Guid, TId>>(Expression.New(constructor, value), value);
    }
}
