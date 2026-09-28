using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;

/// <summary>
/// 직원 한 명을 ID로 조회합니다. 없으면 22001입니다.
/// </summary>
/// <param name="Id">직원 ID 값(API 경로 값).</param>
public sealed record GetEmployeeByIdQuery(Guid Id) : IQuery<EmployeeResponse>;
