using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Validation;
using FluentValidation;
using FluentValidation.Results;
using EmployeeName = EmergencyHub.Employee.Domain.Employees.Name;

namespace EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;

/// <summary>
/// 이름 조회 입력 검증입니다(PRD-002 FR-08). 판정 원본은 <see cref="EmployeeName.Create"/>이고 실패 오류(21007 · 21008 · 21009)를 그대로 필드 <c>Name</c>에 싣습니다.
/// 검증 데코레이터가 <see cref="BuildingBlocks.Domain.Errors.ValidationError"/>(대표 1001, <c>errors.name</c>)로 담습니다(ADR-0018).
/// </summary>
/// <remarks>메시지는 오류의 고정 문구이고, 실패에 입력 값(<c>AttemptedValue</c>)을 싣지 않습니다(NFR-04).</remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 FluentValidation 어셈블리 검색으로 등록해 DI가 만든다(ADR-0017, ADR-0018).")]
internal sealed class GetEmployeeByNameQueryValidator : RequestValidator<GetEmployeeByNameQuery>
{
    public GetEmployeeByNameQueryValidator()
    {
        RuleFor(query => query.Name).Custom((name, context) =>
        {
            var result = EmployeeName.Create(name);
            if (result.IsFailure)
            {
                context.AddFailure(new ValidationFailure(context.PropertyPath, result.Error.Message) { CustomState = result.Error });
            }
        });
    }
}
