namespace EmergencyHub.Employee.Application.Employees.Import;

/// <summary>
/// 파서가 읽은 한 행의 필드 원문입니다. CSV · JSON 파서가 같은 형태로 돌려주고, 필드 규칙은 Handler가 Value Object로 판정합니다(ADR-0026 3 · 7절).
/// </summary>
/// <param name="RowNumber">행 번호(1부터). CSV는 레코드가 시작하는 물리 줄 번호, JSON은 항목 순서(ADR-0026 6절).</param>
/// <param name="Name">name 필드. CSV는 앞뒤 공백을 제거한 값, JSON은 속성이 없으면 <see langword="null"/>.</param>
/// <param name="Email">email 필드.</param>
/// <param name="Tel">tel 필드.</param>
/// <param name="Joined">joined 필드.</param>
internal sealed record ImportRow(int RowNumber, string? Name, string? Email, string? Tel, string? Joined);
