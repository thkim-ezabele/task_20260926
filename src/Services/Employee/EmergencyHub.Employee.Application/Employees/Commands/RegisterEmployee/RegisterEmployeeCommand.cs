using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;

/// <summary>
/// 직원을 등록합니다. 성공하면 새 직원 ID를 돌려줍니다.
/// </summary>
/// <param name="DisplayName">표시 이름(원본). 정규화는 Aggregate가 합니다.</param>
/// <param name="Email">이메일(원본). 정규화는 Aggregate가 합니다.</param>
/// <param name="EmployeeStatus">직원 상태. 누락을 21006으로 구분하려고 nullable입니다(누락 21006, 0 · 99 등은 1002).</param>
public sealed record RegisterEmployeeCommand(string DisplayName, string Email, EmployeeStatus? EmployeeStatus)
    : ICommand<EmployeeId>;
