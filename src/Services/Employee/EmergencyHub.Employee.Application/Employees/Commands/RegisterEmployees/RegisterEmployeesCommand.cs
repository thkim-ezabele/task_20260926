using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;

/// <summary>
/// CSV · JSON 입력으로 직원을 한 번에 등록합니다(PRD-002 FR-06, ADR-0026). 한 행이라도 실패하면 아무것도 저장하지 않습니다.
/// </summary>
/// <remarks>
/// <para>
/// 겉모양(빈 입력 · 출처 조합 · 형식)은 <see cref="RegisterEmployeesCommandValidator"/>가, 파싱 · 행 검증 · 중복 판정은
/// <see cref="RegisterEmployeesCommandHandler"/>가 합니다(ADR-0018 범위 예외, 이 Command에만).
/// </para>
/// <para>
/// 로그 템플릿 인자로 이 Command를 넘기지 않습니다(coding-conventions "로그", BL-130). 로깅 데코레이터는 요청 형식 이름만 남깁니다.
/// </para>
/// </remarks>
/// <param name="Format">내용 형식. 입력이 비어 있으면 <see cref="EmployeeImportFormat.Unknown"/>입니다.</param>
/// <param name="Sources">입력을 꺼낸 곳(비트 조합).</param>
/// <param name="Content">입력 바이트(UTF-8, BOM 허용). 해독은 Application 파서가 합니다.</param>
public sealed record RegisterEmployeesCommand(EmployeeImportFormat Format, EmployeeImportSources Sources, ReadOnlyMemory<byte> Content)
    : ICommand<RegisterEmployeesResponse>;
