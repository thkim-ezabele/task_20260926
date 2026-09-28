using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;

/// <summary>
/// 직원 목록 Handler입니다(PRD-002 FR-07). Read Repository의 목록(<see cref="IEmployeeReadRepository.ListOrderedByJoinedOnAsync"/>)과
/// 개수(<see cref="IEmployeeReadRepository.CountAsync"/>)를 따로 조회해 합칩니다(<c>COUNT(*) OVER()</c> 미사용, ADR-0007).
/// </summary>
/// <remarks>
/// <para>
/// skip = (<see cref="ListEmployeesQuery.Page"/> - 1) × <see cref="ListEmployeesQuery.PageSize"/>를 <c>checked</c>로 계산합니다. Validator를 지난 값의 최댓값은
/// 9,999,900이고, 검증을 거치지 않은 값이 넘치면 음수 skip으로 조회하지 않고 <see cref="OverflowException"/>(예상하지 못한 오류)입니다.
/// </para>
/// <para>
/// 두 조회는 같은 읽기 DbContext라 차례로 실행하고 트랜잭션으로 묶지 않습니다. 그 사이 등록이 끼면 <c>totalCount</c>가 목록보다 앞설 수 있습니다(읽기 일관성 요구 없음).
/// 마지막 쪽을 넘는 쪽 번호는 빈 목록 + 올바른 <c>totalCount</c>인 성공입니다.
/// </para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 Scrutor 어셈블리 검색으로 IQueryHandler<,>에 등록해 DI가 만든다(ADR-0015, ADR-0017).")]
internal sealed class ListEmployeesQueryHandler(IEmployeeReadRepository repository) : IQueryHandler<ListEmployeesQuery, ListEmployeesResponse>
{
    public async Task<Result<ListEmployeesResponse>> Handle(ListEmployeesQuery query, CancellationToken cancellationToken)
    {
        var skip = checked((query.Page - 1) * query.PageSize);

        var contacts = await repository.ListOrderedByJoinedOnAsync(skip, query.PageSize, cancellationToken);
        var totalCount = await repository.CountAsync(cancellationToken);

        return new ListEmployeesResponse([.. contacts.Select(EmployeeResponse.From)], totalCount, query.Page, query.PageSize);
    }
}
