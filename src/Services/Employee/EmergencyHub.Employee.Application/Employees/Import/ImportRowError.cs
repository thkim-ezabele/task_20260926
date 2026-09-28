using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.Employee.Application.Employees.Import;

/// <summary>
/// 파서가 찾은 행 오류입니다. Handler가 경로 <c>Rows[n]</c>의 필드 오류로 옮깁니다(ADR-0026 6절).
/// </summary>
/// <param name="RowNumber">행 번호(1부터, <see cref="ImportRow.RowNumber"/>와 같은 규칙).</param>
/// <param name="Error">행 오류(CSV 21019 · 21020 · 21021 등). 메시지는 고정 문구이고 입력 값을 담지 않습니다.</param>
internal sealed record ImportRowError(int RowNumber, Error Error);
