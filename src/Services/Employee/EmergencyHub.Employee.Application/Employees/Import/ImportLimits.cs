namespace EmergencyHub.Employee.Application.Employees.Import;

/// <summary>
/// 일괄 가져오기 입력의 상한입니다(PRD-002 NFR-01, ADR-0026 3절).
/// </summary>
internal static class ImportLimits
{
    /// <summary>한 요청의 최대 행 수. 넘으면 요청 전체 오류 21027입니다. 빈 줄은 세지 않고 행 오류가 난 행은 셉니다.</summary>
    public const int MaxRows = 1000;
}
