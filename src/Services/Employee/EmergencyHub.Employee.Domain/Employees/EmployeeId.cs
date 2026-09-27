using EmergencyHub.BuildingBlocks.Domain.Identifiers;

namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 식별자(UUID v7)입니다. 값은 Handler가 <c>IIdGenerator</c>로 만듭니다(ADR-0013).
/// </summary>
/// <param name="Value">감싼 UUID 값.</param>
/// <remarks>값 변환기 · 키 <c>ValueGeneratedNever</c>는 EF Core 공통 규칙이 겁니다(TD-015).</remarks>
public readonly record struct EmployeeId(Guid Value) : IStronglyTypedId<EmployeeId>;
