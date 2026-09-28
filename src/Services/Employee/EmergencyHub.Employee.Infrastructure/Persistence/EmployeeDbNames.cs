using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

namespace EmergencyHub.Employee.Infrastructure.Persistence;

/// <summary>
/// Employee DB 객체 이름 상수의 단일 위치입니다(database.md Employee "새 스키마 명세" · 이름 상수 위치).
/// </summary>
/// <remarks>
/// 매핑(<c>ToTable</c> · <c>HasUniqueIndex</c>)과 UnitOfWork 23505 매핑(<c>errors.Map</c>)이 같은 상수를 참조합니다(문자열 리터럴 금지).
/// <c>pk_</c> · <c>ck_</c> · <c>ix_</c> 이름은 명명 규칙 · 공통 도우미가 최종 컬럼 이름으로 만들므로 여기 두지 않습니다
/// (<c>ix_</c> 이름과 열 순서는 모델 메타데이터 테스트가 고정).
/// </remarks>
public static class EmployeeDbNames
{
    /// <summary>
    /// 직원 테이블 이름입니다. <c>DbSet</c> 없이 <c>Set&lt;Employee&gt;()</c>만 쓰면 규칙 이름이 단수(<c>employee</c>)가 되므로 명시합니다.
    /// </summary>
    public const string EmployeesTable = "employees";

    /// <summary>
    /// 정규화 이메일 유니크 인덱스입니다(ADR-0027). 위반(23505)은 <c>EmployeeErrors.DuplicateEmail</c>(23001)로 바뀝니다.
    /// </summary>
    public static readonly UniqueIndexName NormalizedEmailUniqueIndex = new("ux_employees_normalized_email");
}
