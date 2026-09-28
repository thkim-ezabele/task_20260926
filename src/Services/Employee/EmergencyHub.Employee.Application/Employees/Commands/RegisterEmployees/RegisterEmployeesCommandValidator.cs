using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.Employee.Domain.Employees;
using FluentValidation;

namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;

/// <summary>
/// 일괄 등록 요청의 겉모양 검증입니다(PRD-002 FR-05 · FR-06, ADR-0026 7절). 내용(파싱 · 필드 규칙 · 중복)은 보지 않고 DB에 접근하지 않습니다.
/// </summary>
/// <remarks>
/// 규칙 선언 순서가 판정 순서이고 첫 실패 하나에서 멈춥니다(<see cref="CascadeMode.Stop"/>):
/// <list type="number">
/// <item>빈 입력 21028(경로 ""): <see cref="RegisterEmployeesCommand.Sources"/>가 <see cref="EmployeeImportSources.None"/>이거나,
/// <see cref="RegisterEmployeesCommand.Content"/>가 길이 0이거나 맨 앞 BOM(<c>EF BB BF</c>) 하나를 뗀 뒤 0x20 · 0x09 · 0x0D · 0x0A만 있음.</item>
/// <item>Sources에 정의 안 된 비트 1002(경로 <c>Sources</c>, <c>MustBeDefinedEnum</c>).</item>
/// <item>Sources 비트가 둘 이상 21029(경로 "").</item>
/// <item>Format이 정의 안 된 값(0 포함) 1002(경로 <c>Format</c>). 빈 입력이면 1에서 멈추므로 보고하지 않습니다(ADR-0026 2절).</item>
/// </list>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 FluentValidation 어셈블리 검색으로 등록해 DI가 만든다(ADR-0017, ADR-0018).")]
internal sealed class RegisterEmployeesCommandValidator : RequestValidator<RegisterEmployeesCommand>
{
    private static readonly SearchValues<byte> BlankBytes = SearchValues.Create([0x20, 0x09, 0x0D, 0x0A]);

    public RegisterEmployeesCommandValidator()
    {
        // 요청 전체 오류는 하나만 보고한다. 앞 규칙이 실패하면 뒤 규칙을 실행하지 않는다.
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(command => command)
            .Must(command => !IsEmptyInput(command))
            .WithError(EmployeeErrors.ImportInputEmpty)
            .OverridePropertyName(string.Empty);

        RuleFor(command => command.Sources)
            .MustBeDefinedEnum();

        RuleFor(command => command.Sources)
            .Must(sources => BitOperations.PopCount((uint)sources) <= 1)
            .WithError(EmployeeErrors.ImportMultipleSources)
            .OverridePropertyName(string.Empty);

        RuleFor(command => command.Format)
            .MustBeDefinedEnum();
    }

    private static bool IsEmptyInput(RegisterEmployeesCommand command) =>
        command.Sources == EmployeeImportSources.None || IsBlank(command.Content.Span);

    private static bool IsBlank(ReadOnlySpan<byte> content)
    {
        ReadOnlySpan<byte> bom = [0xEF, 0xBB, 0xBF];
        var body = content.StartsWith(bom) ? content[bom.Length..] : content;

        return body.IndexOfAnyExcept(BlankBytes) < 0;
    }
}
