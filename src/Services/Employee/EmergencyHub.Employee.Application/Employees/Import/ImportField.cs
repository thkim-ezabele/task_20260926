namespace EmergencyHub.Employee.Application.Employees.Import;

/// <summary>
/// 행 오류가 가리키는 필드입니다. Handler가 오류 경로(<c>Rows[n]</c> 또는 <c>Rows[n].Joined</c> 등)를 고를 때 씁니다(ADR-0026 6절).
/// </summary>
internal enum ImportField : short
{
    /// <summary>특정 필드가 아님(행 전체 오류, 경로 <c>Rows[n]</c>).</summary>
    None = 0,

    /// <summary>name 필드(<c>Rows[n].Name</c>).</summary>
    Name = 1,

    /// <summary>email 필드(<c>Rows[n].Email</c>).</summary>
    Email = 2,

    /// <summary>tel 필드(<c>Rows[n].Tel</c>).</summary>
    Tel = 3,

    /// <summary>joined 필드(<c>Rows[n].Joined</c>).</summary>
    Joined = 4,
}
