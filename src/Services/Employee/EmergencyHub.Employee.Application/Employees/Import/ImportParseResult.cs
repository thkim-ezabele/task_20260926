namespace EmergencyHub.Employee.Application.Employees.Import;

/// <summary>
/// 파싱 결과입니다. 요청 전체 오류(21022 · 21027 등)는 이 형식 대신 실패 <c>Result</c>로 돌려줍니다.
/// </summary>
/// <param name="Rows">읽은 행(행 번호 오름차순). 행 오류가 난 행은 들어가지 않습니다.</param>
/// <param name="Errors">행 오류(행 번호 오름차순, 행마다 첫 오류 하나).</param>
internal sealed record ImportParseResult(IReadOnlyList<ImportRow> Rows, IReadOnlyList<ImportRowError> Errors);
