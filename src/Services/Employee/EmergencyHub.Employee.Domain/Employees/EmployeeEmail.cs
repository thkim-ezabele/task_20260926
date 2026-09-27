namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// 직원 이메일의 정규화와 형식 판정입니다. Aggregate 불변식과 Application Validator가 같은 판정을 쓰도록 한곳에 둡니다.
/// </summary>
/// <remarks>
/// <para>
/// 이메일은 값 객체가 아니라 정규화한 <see cref="string"/> 속성입니다(S03 계획 리뷰 결정). 정규화는 앞뒤 공백 제거 +
/// <see cref="string.ToLowerInvariant"/>이고, DB는 이 값에 일반 유니크 인덱스 <c>ux_employees_email</c>을 겁니다
/// (citext · <c>lower()</c> 식 인덱스는 쓰지 않음).
/// </para>
/// <para>
/// 형식 판정은 FluentValidation <c>EmailAddress()</c>의 기본 모드(AspNetCoreCompatible)와 같은 수준입니다:
/// <c>@</c>가 정확히 하나이고 앞뒤 부분이 비어 있지 않아야 합니다. 도메인 판정과 요청 검증이 어긋나 500이 되지 않도록 이 판정 하나만 씁니다.
/// </para>
/// </remarks>
public static class EmployeeEmail
{
    /// <summary>
    /// 앞뒤 공백을 지우고 문화권과 무관한 소문자로 바꿉니다. 여러 번 적용해도 결과가 같습니다.
    /// </summary>
    /// <param name="email">원본 이메일.</param>
    /// <returns>정규화한 이메일. 공백만 있으면 빈 문자열입니다.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="email"/>이 <see langword="null"/>인 경우.</exception>
    /// <remarks>
    /// <see cref="string.ToLowerInvariant"/>는 UTF-16 코드 단위를 1:1로 바꾸므로 길이가 바뀌지 않고, 현재 문화권(예: tr-TR)의 영향을 받지 않습니다.
    /// 터키어 'İ'(U+0130)는 바꾸지 않고 그대로 둡니다(.NET 고정 문화권 규칙, 단위 테스트로 고정).
    /// </remarks>
    public static string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// <c>@</c>가 정확히 하나이고 그 앞뒤가 비어 있지 않으면 <see langword="true"/>입니다.
    /// </summary>
    /// <param name="email">판정할 이메일(보통 정규화한 값).</param>
    /// <returns>형식이 맞으면 <see langword="true"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="email"/>이 <see langword="null"/>인 경우.</exception>
    public static bool IsWellFormed(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var at = email.IndexOf('@', StringComparison.Ordinal);

        return at > 0 && at < email.Length - 1 && at == email.LastIndexOf('@');
    }
}
