using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;

/// <summary>
/// 직원 단건 조회 Handler입니다. Read Repository로만 조회합니다(ADR-0007).
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 Scrutor 어셈블리 검색으로 IQueryHandler<,>에 등록해 DI가 만든다(ADR-0015, ADR-0017).")]
internal sealed class GetEmployeeByIdQueryHandler(IEmployeeReadRepository readRepository)
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeResponse>
{
    public async Task<Result<EmployeeResponse>> Handle(GetEmployeeByIdQuery query, CancellationToken cancellationToken)
    {
        var employee = await readRepository.GetByIdAsync(query.Id, cancellationToken);
        if (employee is null)
        {
            return EmployeeErrors.NotFound;
        }

        return employee;
    }
}
