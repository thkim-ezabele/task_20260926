using System.Text;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S07-T03 도구(developer): 이름 값이 남을 수 있는 형태 목록(NFC · NFD 원문, 대문자 · 소문자 퍼센트 인코딩). 완료 조건 ④ 본 테스트는 tester가 쓴다.
public sealed class PersonalValueFormsTests
{
    // ---- 성공 ----

    [Fact]
    public void Name_Korean_ReturnsNfcNfdAndBothPercentEncodingCases()
    {
        var nfc = "직원42".Normalize(NormalizationForm.FormC);
        var nfd = "직원42".Normalize(NormalizationForm.FormD);

        var forms = PersonalValueForms.Name(nfd);

        forms.Should().Equal(
            nfc,
            nfd,
            Uri.EscapeDataString(nfc),
            Uri.EscapeDataString(nfc).ToLowerInvariant(),
            Uri.EscapeDataString(nfd),
            Uri.EscapeDataString(nfd).ToLowerInvariant());
        forms[2].Should().StartWith("%EC", "EscapeDataString은 대문자 16진수");
    }

    // ---- 실패 ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_EmptyOrWhitespace_Throws(string name)
    {
        var act = () => PersonalValueForms.Name(name);

        act.Should().Throw<ArgumentException>("찾을 값이 없으면 부재 단언이 항상 통과한다");
    }

    // ---- 엣지 ----

    [Fact]
    public void Name_AsciiWithoutEscapes_CollapsesDuplicateForms()
    {
        PersonalValueForms.Name("Kim").Should().Equal(["Kim", "kim"], "ASCII는 NFC = NFD = 인코딩 결과라 원문과 소문자 두 형태만 남는다");
    }
}
