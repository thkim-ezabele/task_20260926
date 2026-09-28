namespace EmergencyHub.Employee.Application.Employees;

/// <summary>
/// 조회 API의 직원 항목입니다(PRD-002 FR-07 목록 항목, FR-08 이름 단건). JSON 이름은 <c>id</c> · <c>name</c> · <c>email</c> · <c>tel</c> · <c>joined</c>입니다.
/// </summary>
/// <remarks>
/// 목록 · 이름 조회 두 Query가 함께 쓰므로 기능 폴더 밖에 둡니다(coding-conventions "기능 폴더 구조").
/// Read Repository 프로젝션 <see cref="EmployeeContactResponse"/>의 이름(<c>PhoneNumber</c> · <c>JoinedOn</c>)을 API 이름으로 옮깁니다.
/// </remarks>
/// <param name="Id">직원 ID.</param>
/// <param name="Name">이름(Trim + NFC 저장 값).</param>
/// <param name="Email">이메일 입력 표기(정규화 값 아님).</param>
/// <param name="Tel">전화번호(입력 그대로).</param>
/// <param name="Joined">입사일(JSON <c>yyyy-MM-dd</c>).</param>
public sealed record EmployeeResponse(Guid Id, string Name, string Email, string Tel, DateOnly Joined)
{
    /// <summary>Read Repository 프로젝션을 응답 항목으로 옮깁니다.</summary>
    /// <param name="contact">Read Repository가 돌려준 직원 연락정보.</param>
    /// <returns>응답 항목.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contact"/>가 <see langword="null"/>인 경우.</exception>
    public static EmployeeResponse From(EmployeeContactResponse contact)
    {
        ArgumentNullException.ThrowIfNull(contact);

        return new EmployeeResponse(contact.Id, contact.Name, contact.Email, contact.PhoneNumber, contact.JoinedOn);
    }
}
