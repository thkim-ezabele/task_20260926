namespace EmergencyHub.Employee.Domain.Employees;

/// <summary>
/// Employee Value Object가 함께 쓰는 문자 판정입니다. <see cref="Name"/>(21009)과 <see cref="Email"/>(21004)이 같은 기준을 씁니다(PRD-002 FR-01, BL-129).
/// </summary>
internal static class EmployeeTextRules
{
    /// <summary>
    /// 제어 문자(Cc, <see cref="char.IsControl(char)"/>: U+0000 ~ U+001F · U+007F ~ U+009F, 탭 · DEL 포함) 또는 짝 없는 서로게이트가 있는지 봅니다.
    /// </summary>
    /// <remarks>서식 문자(Cf, ZWJ 등)는 제어 문자가 아니므로 거부하지 않습니다. 짝을 이룬 서로게이트(이모지 등)는 허용합니다.</remarks>
    /// <param name="value">검사할 문자열.</param>
    /// <returns>허용하지 않는 문자가 하나라도 있으면 <see langword="true"/>.</returns>
    public static bool ContainsControlOrUnpairedSurrogate(ReadOnlySpan<char> value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsControl(current) || char.IsLowSurrogate(current))
            {
                return true;
            }

            if (char.IsHighSurrogate(current))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                {
                    return true;
                }

                i++;
            }
        }

        return false;
    }
}
