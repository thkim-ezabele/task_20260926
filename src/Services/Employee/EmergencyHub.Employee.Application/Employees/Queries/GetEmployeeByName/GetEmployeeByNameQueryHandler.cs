using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;

/// <summary>
/// 이름 조회 Handler입니다(PRD-002 FR-08). 입력을 <see cref="Name.Create"/>(Trim + NFC)로 바꿔
/// Read Repository의 정확 일치 조회(<see cref="IEmployeeReadRepository.FindFirstByNameAsync"/>)에 넘깁니다.
/// </summary>
/// <remarks>
/// 동명이인의 순서(<c>joined_on</c> → <c>id</c>)와 첫 1명 선택은 Repository 쿼리가 하고, Handler는 다시 고르지 않습니다.
/// 없으면 404 · 22001(<see cref="EmployeeErrors.NotFound"/>)입니다. 검증을 거치지 않은 잘못된 이름은 조회하지 않고 <see cref="Name.Create"/>의 오류를 돌려줍니다.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 Scrutor 어셈블리 검색으로 IQueryHandler<,>에 등록해 DI가 만든다(ADR-0015, ADR-0017).")]
internal sealed class GetEmployeeByNameQueryHandler(IEmployeeReadRepository repository) : IQueryHandler<GetEmployeeByNameQuery, EmployeeResponse>
{
    public async Task<Result<EmployeeResponse>> Handle(GetEmployeeByNameQuery query, CancellationToken cancellationToken)
    {
        var name = Name.Create(query.Name);
        if (name.IsFailure)
        {
            return name.Error;
        }

        var contact = await repository.FindFirstByNameAsync(name.Value, cancellationToken);
        if (contact is null)
        {
            return EmployeeErrors.NotFound;
        }

        return EmployeeResponse.From(contact);
    }
}
