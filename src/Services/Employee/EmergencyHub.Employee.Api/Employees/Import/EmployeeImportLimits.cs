namespace EmergencyHub.Employee.Api.Employees.Import;

/// <summary>
/// 일괄 등록 요청의 크기 한도입니다(PRD-002 NFR-01, ADR-0025). 요청 본문 전체 기준 1 MiB입니다.
/// </summary>
/// <remarks>
/// 액션 특성(<c>RequestSizeLimit</c> · <c>RequestFormLimits.MultipartBodyLengthLimit</c> · <c>ValueLengthLimit</c>)에 이 값을 쓰고,
/// 바인더는 그 특성을 엔드포인트 메타데이터로 읽어 검사합니다. 행 수 한도(1,000행 초과 21027)는 Application 파서 몫입니다.
/// </remarks>
internal static class EmployeeImportLimits
{
    /// <summary>요청 본문 · multipart 본문 · 폼 값 하나의 최대 바이트 수(1 MiB = 1,048,576).</summary>
    public const int MaxRequestBodyBytes = 1024 * 1024;
}
