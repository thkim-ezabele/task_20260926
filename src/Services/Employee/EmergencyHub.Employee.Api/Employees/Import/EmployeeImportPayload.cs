using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.Employee.Api.Employees.Import;

/// <summary>
/// 일괄 등록 요청에서 전용 바인더(<see cref="EmployeeImportPayloadBinder"/>)가 꺼낸 입력입니다(ADR-0026 1절, PRD-002 FR-05).
/// Controller가 <see cref="RegisterEmployeesCommand"/>로 그대로 옮깁니다.
/// </summary>
/// <remarks>로그 템플릿 인자로 넘기지 않습니다(입력에 개인정보가 들어 있음, NFR-04).</remarks>
/// <param name="Format">판별한 내용 형식. 입력이 비었으면 <see cref="EmployeeImportFormat.Unknown"/>.</param>
/// <param name="Sources">입력을 찾은 곳(찾은 출처 비트를 모두 켬). 조합 판정은 Validator가 합니다.</param>
/// <param name="Content">입력 바이트(해독 · 치환하지 않은 원래 바이트).</param>
[ModelBinder(typeof(EmployeeImportPayloadBinder))]
public sealed record EmployeeImportPayload(EmployeeImportFormat Format, EmployeeImportSources Sources, ReadOnlyMemory<byte> Content);
