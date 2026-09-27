using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.Employee.Domain.Employees;
using FluentValidation;

namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;

/// <summary>
/// 직원 등록 요청 검증입니다(21001 ~ 21006, 코드값 1002). DB에 접근하지 않습니다(중복은 Handler).
/// </summary>
/// <remarks>
/// 길이 · 형식은 Aggregate와 같은 기준(앞뒤 공백 제거 뒤 <see cref="string.Length"/>, <see cref="EmployeeEmail.IsWellFormed"/>)으로 잽니다.
/// 기준이 어긋나면 Validator를 통과한 요청이 도메인 예외(500)가 되기 때문입니다.
/// 규칙 순서(한 속성 안에서 첫 실패에서 멈춤): 이름 필수 → 길이, 이메일 필수 → 길이 → 형식, 상태 필수 → 정의값.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 FluentValidation 어셈블리 검색으로 등록해 DI가 만든다(ADR-0017, ADR-0018).")]
internal sealed class RegisterEmployeeCommandValidator : RequestValidator<RegisterEmployeeCommand>
{
    public RegisterEmployeeCommandValidator()
    {
        RuleFor(command => command.DisplayName)
            .NotEmpty().WithError(EmployeeErrors.DisplayNameRequired)
            .Must(displayName => displayName.Trim().Length <= Domain.Employees.Employee.DisplayNameMaxLength)
            .WithError(EmployeeErrors.DisplayNameTooLong);

        RuleFor(command => command.Email)
            .NotEmpty().WithError(EmployeeErrors.EmailRequired)
            .Must(email => EmployeeEmail.Normalize(email).Length <= Domain.Employees.Employee.EmailMaxLength)
            .WithError(EmployeeErrors.EmailTooLong)
            .Must(email => EmployeeEmail.IsWellFormed(EmployeeEmail.Normalize(email)))
            .WithError(EmployeeErrors.EmailInvalid);

        RuleFor(command => command.EmployeeStatus)
            .NotNull().WithError(EmployeeErrors.EmployeeStatusRequired);

        // 누락과 정의되지 않은 값을 다른 코드로 알리려고 값이 있을 때만 정의값 규칙을 건다.
        When(command => command.EmployeeStatus.HasValue, () =>
            RuleFor(command => command.EmployeeStatus.GetValueOrDefault())
                .MustBeDefinedEnum()
                .OverridePropertyName(nameof(RegisterEmployeeCommand.EmployeeStatus)));
    }
}
