namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;

/// <summary>
/// 일괄 등록 입력을 꺼낸 곳(비트 조합)입니다(ADR-0026 1절, ADR-0008). DB에 저장하지 않는 Command 입력 코드입니다.
/// </summary>
/// <remarks>
/// 바인더가 찾은 출처를 모두 켜서 넘기고, 조합 판정(없음 21028 · 정의 안 된 비트 1002 · 둘 이상 21029)은 Validator가 합니다(PRD-002 FR-05 · FR-06).
/// </remarks>
[Flags]
public enum EmployeeImportSources : int
{
    /// <summary>출처 없음(빈 입력).</summary>
    None = 0,

    /// <summary>multipart 파일 필드 <c>file</c>.</summary>
    File = 1 << 0,

    /// <summary>폼 텍스트 필드 <c>data</c>(multipart · form-urlencoded).</summary>
    Data = 1 << 1,

    /// <summary>raw body(<c>text/csv</c> · <c>application/json</c>).</summary>
    Body = 1 << 2,
}
