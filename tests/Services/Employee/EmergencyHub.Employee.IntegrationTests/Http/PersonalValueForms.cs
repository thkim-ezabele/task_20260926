using System.Text;

namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>
/// 경로 매개변수로 들어온 개인정보 값이 응답 · 로그 · span에 남을 수 있는 형태 목록입니다(PRD-002 NFR-04, ADR-0025, S07-T03).
/// </summary>
/// <remarks>
/// <see cref="PersonalDataScan"/>은 대소문자 무시 서수 비교라 유니코드 정규화를 하지 않습니다. NFC로 찾으면 NFD 원문을 놓치므로 두 형태를 모두 넣고,
/// 퍼센트 인코딩(<see cref="Uri.EscapeDataString(string)"/>의 대문자 16진수와 소문자 16진수)도 넣습니다.
/// Api.UnitTests의 <c>GetEmployeeByNameEncodedFormsHttpTests.ForbiddenForms</c>와 같은 목록입니다(프로젝트가 달라 공용 코드 없이 같은 규칙으로 둠).
/// </remarks>
public static class PersonalValueForms
{
    /// <summary>
    /// 이름의 NFC · NFD 원문과 각각의 대문자 · 소문자 퍼센트 인코딩을 돌려줍니다(이 순서, 서수 중복 제거: ASCII 이름은 NFC = NFD라 줄어듦).
    /// </summary>
    /// <param name="name">이름 원문(정규화 형태 무관).</param>
    /// <returns>찾을 형태 목록.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/>이 비었거나 공백뿐인 경우(찾을 값이 없어 부재 단언이 항상 통과함).</exception>
    public static IReadOnlyList<string> Name(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var nfc = name.Normalize(NormalizationForm.FormC);
        var nfd = name.Normalize(NormalizationForm.FormD);
        string[] forms =
        [
            nfc,
            nfd,
            Uri.EscapeDataString(nfc),
            Uri.EscapeDataString(nfc).ToLowerInvariant(),
            Uri.EscapeDataString(nfd),
            Uri.EscapeDataString(nfd).ToLowerInvariant(),
        ];

        return [.. forms.Distinct(StringComparer.Ordinal)];
    }
}
