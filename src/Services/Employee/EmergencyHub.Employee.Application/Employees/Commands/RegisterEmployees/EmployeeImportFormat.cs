namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;

/// <summary>
/// 일괄 등록 입력의 내용 형식입니다(ADR-0026 1 · 2절, ADR-0008). DB에 저장하지 않는 Command 입력 코드입니다.
/// </summary>
/// <remarks>
/// 바인더가 입력이 비어 있어 형식을 정하지 못하면 <see cref="Unknown"/>(0)을 넘깁니다. Validator는 빈 입력(21028)을 먼저 보고하고,
/// 입력이 있는데 정의되지 않은 값(0 포함)이면 1002로 거부합니다.
/// </remarks>
public enum EmployeeImportFormat : short
{
    /// <summary>예약 값(형식 미정). 업무 값으로 쓰지 않습니다.</summary>
    Unknown = 0,

    /// <summary>CSV(PRD-002 FR-03).</summary>
    Csv = 1,

    /// <summary>JSON(PRD-002 FR-04).</summary>
    Json = 2,
}
