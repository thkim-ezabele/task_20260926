using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using FluentValidation;

namespace EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;

/// <summary>
/// 목록 페이징 범위 검증입니다(PRD-002 FR-07, ADR-0025). 범위 밖이면 필드마다 1003(<see cref="CommonErrors.InvalidPaging"/>)이고,
/// 검증 데코레이터가 <see cref="ValidationError"/>(대표 1001, 필드 경로 <c>Page</c> · <c>PageSize</c>)로 담습니다(ADR-0018).
/// </summary>
/// <remarks>메시지는 오류의 고정 문구라 입력 값이 들어가지 않습니다(<c>InclusiveBetween</c> 기본 메시지의 <c>{PropertyValue}</c>를 덮음).</remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 FluentValidation 어셈블리 검색으로 등록해 DI가 만든다(ADR-0017, ADR-0018).")]
internal sealed class ListEmployeesQueryValidator : RequestValidator<ListEmployeesQuery>
{
    public ListEmployeesQueryValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(ListEmployeesQuery.DefaultPage, ListEmployeesQuery.MaxPage)
            .WithError(CommonErrors.InvalidPaging);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(ListEmployeesQuery.MinPageSize, ListEmployeesQuery.MaxPageSize)
            .WithError(CommonErrors.InvalidPaging);
    }
}
