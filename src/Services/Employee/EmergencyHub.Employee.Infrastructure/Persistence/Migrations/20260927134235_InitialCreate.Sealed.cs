namespace EmergencyHub.Employee.Infrastructure.Persistence.Migrations;

/// <summary>
/// 초기 마이그레이션(<c>employees</c> 테이블 · <c>ux_employees_email</c> · <c>ck_employees_employee_status</c>).
/// </summary>
/// <remarks>
/// coding-conventions "클래스는 기본 sealed"를 지키기 위해 직접 작성한 partial 선언이다. 생성 파일
/// (<c>20260927134235_InitialCreate.cs</c> · <c>.Designer.cs</c>)은 고치지 않는다. 마이그레이션을 다시 만들거나
/// (ADR-0012 리셋) 새로 추가하면 파일 이름을 <c>&lt;마이그레이션 ID&gt;.Sealed.cs</c>로 맞춰 함께 둔다(database.md 마이그레이션 규칙).
/// </remarks>
public sealed partial class InitialCreate;
