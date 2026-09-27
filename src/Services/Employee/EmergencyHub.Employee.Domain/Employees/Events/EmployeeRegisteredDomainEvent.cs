using EmergencyHub.BuildingBlocks.Domain.Events;

namespace EmergencyHub.Employee.Domain.Employees.Events;

/// <summary>
/// 직원이 등록되었습니다. 지금은 Aggregate가 수집만 하고 커밋 뒤 UnitOfWork가 비웁니다(ADR-0014, ADR-0023).
/// </summary>
/// <param name="EmployeeId">등록된 직원 ID. 개인정보(이름 · 이메일)는 담지 않습니다.</param>
public sealed record EmployeeRegisteredDomainEvent(EmployeeId EmployeeId) : IDomainEvent;
