using System.Text;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// S05-T03 Name Value Object(PRD-002 FR-01, ADR-0026 8절): Trim → 필수(21007) → 제어 문자 · 짝 없는 서로게이트(21009) → NFC → 길이(21008).
// 길이는 NFC 뒤 string.Length(UTF-16 코드 단위) 1 ~ 100이다. 이모지는 한 글자가 2로 센다.
[Trait("FR", "PRD-002/FR-01")]
[Trait("NFR", "PRD-002/NFR-05")]
public sealed class NameTests
{
    public static TheoryData<string?> BlankValues() => new(null, string.Empty, " ", "   ", "\t", "\r\n", "　", "   ");

    public static TheoryData<string> NamesAtMaxLength() => new(
        new string('a', 100),
        new string('가', 100),
        string.Concat(Enumerable.Repeat("😀", 50)),
        "  " + new string('가', 100) + "  ",
        string.Concat(Enumerable.Repeat("é", 100)));

    public static TheoryData<string> NamesOverMaxLength() => new(
        new string('a', 101),
        new string('가', 101),
        "a" + string.Concat(Enumerable.Repeat("😀", 50)),
        string.Concat(Enumerable.Repeat("é", 101)));

    public static TheoryData<string> NamesWithControlCharacter() => new(
        "홍\t길동",
        "홍\n길동",
        "홍\r길동",
        "홍\0길동",
        "홍\u001F길동",
        "홍\u007F길동",
        "홍\u0085길동",
        "홍\u009F길동");

    public static TheoryData<string> NamesWithUnpairedSurrogate() => new(
        "홍\uD83D길동",
        "홍\uDE00길동",
        "\uD83D",
        "\uDE00",
        "홍길동\uD83D",
        "\uDE00\uD83D");

    // ---- 성공 ----

    [Theory]
    [InlineData("홍길동", "홍길동")]
    [InlineData("  홍길동  ", "홍길동")]
    [InlineData("\t홍길동\r\n", "홍길동")]
    [InlineData("홍 길동", "홍 길동")]
    [InlineData("Hong Gil-dong", "Hong Gil-dong")]
    [InlineData("a", "a")]
    public void Create_ValidName_ReturnsTrimmedValue(string input, string expected)
    {
        var result = Name.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Fact]
    public void Create_NfdName_NormalizesToNfc()
    {
        // "é"(U+00E9)의 NFD는 e + U+0301, "가"(U+AC00)의 NFD는 U+1100 U+1161.
        var result = Name.Create("José 가");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("José 가");
        result.Value.Value.IsNormalized(NormalizationForm.FormC).Should().BeTrue();
    }

    [Fact]
    public void Create_NfdAndNfcOfSameName_AreEqual()
    {
        Name.Create("José").Value.Should().Be(Name.Create("José").Value);
    }

    [Theory]
    [MemberData(nameof(NamesAtMaxLength))]
    public void Create_AtMaxLengthAfterTrimAndNfc_Succeeds(string input)
    {
        var result = Name.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Length.Should().Be(Name.MaxLength);
    }

    [Theory]
    [InlineData("홍‍길동")]
    [InlineData("​홍길동")]
    [InlineData("👨‍👩‍👧")]
    [InlineData("﻿홍길동")]
    public void Create_FormatCharacter_IsAllowed(string input)
    {
        // Cf(ZWJ · ZWSP · BOM 등)는 제어 문자(Cc)가 아니므로 허용한다(인계 메모). Trim은 U+200B · U+FEFF를 지우지 않는다.
        var result = Name.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(input);
    }

    [Fact]
    public void Create_PairedSurrogate_IsAllowed()
    {
        var result = Name.Create("홍길동😀");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("홍길동😀");
    }

    [Fact]
    public void Create_SameValue_AreEqual()
    {
        Name.Create(" 홍길동 ").Value.Should().Be(Name.Create("홍길동").Value);
    }

    [Fact]
    public void Create_DifferentValue_AreNotEqual()
    {
        Name.Create("홍길동").Value.Should().NotBe(Name.Create("홍길순").Value);
    }

    // ---- 실패: 필수(21007) ----

    [Theory]
    [MemberData(nameof(BlankValues))]
    public void Create_NullEmptyOrWhitespace_ReturnsNameRequired(string? input)
    {
        var result = Name.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.NameRequired);
    }

    // ---- 실패: 길이(21008) ----

    [Theory]
    [MemberData(nameof(NamesOverMaxLength))]
    public void Create_OverMaxLengthAfterTrimAndNfc_ReturnsNameTooLong(string input)
    {
        var result = Name.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.NameTooLong);
    }

    [Fact]
    public void Create_NfcExpandsOverMaxLength_ReturnsNameTooLong()
    {
        // U+0958(DEVANAGARI QA)는 합성 제외 문자라 NFC에서도 U+0915 U+093C 두 코드 단위가 된다. 길이는 NFC 뒤 값으로 잰다.
        var input = new string('क़', 51);

        "क़".Normalize(NormalizationForm.FormC).Length.Should().Be(2);

        var result = Name.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.NameTooLong);
    }

    // ---- 실패: 허용하지 않는 문자(21009) ----

    [Theory]
    [MemberData(nameof(NamesWithControlCharacter))]
    public void Create_ControlCharacterInside_ReturnsNameInvalidCharacter(string input)
    {
        var result = Name.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.NameInvalidCharacter);
    }

    [Theory]
    // 짝 없는 서로게이트는 발견 단계 직렬화에서 값이 바뀔 수 있어(짝 없는 상위 서로게이트가 통과하는 것을 실측) 발견 단계 열거를 끈다.
    [MemberData(nameof(NamesWithUnpairedSurrogate), DisableDiscoveryEnumeration = true)]
    public void Create_UnpairedSurrogate_ReturnsNameInvalidCharacterWithoutThrowing(string input)
    {
        // string.Normalize는 짝 없는 서로게이트에서 ArgumentException을 던진다. Normalize 전에 검사해 Result로 돌려준다.
        input.Any(char.IsSurrogate).Should().BeTrue("테스트 데이터가 직렬화로 바뀌지 않고 서로게이트를 담아야 한다");
        var act = () => Name.Create(input);

        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.NameInvalidCharacter);
    }

    // ---- 엣지: 판정 순서 ----

    [Fact]
    public void Create_OverMaxLengthWithControlCharacter_ReportsInvalidCharacterFirst()
    {
        var result = Name.Create(new string('a', 101) + "\t" + "a");

        result.Error.Should().BeSameAs(EmployeeErrors.NameInvalidCharacter);
    }

    [Fact]
    public void Create_ControlCharacterOnlyAtEdges_IsTrimmedAndSucceeds()
    {
        // 탭 · 줄바꿈은 공백이기도 해서 앞뒤에 있으면 Trim으로 지워진다. 안쪽에 남은 제어 문자만 거부한다.
        var result = Name.Create("\n\t홍길동\t\n");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("홍길동");
    }

    [Fact]
    public void Create_ControlCharacterOnlyThatIsNotWhitespace_ReturnsNameInvalidCharacter()
    {
        // U+0000은 공백이 아니라 Trim되지 않는다. 공백만이 아니므로 필수가 아니라 문자 오류다.
        var result = Name.Create("\0");

        result.Error.Should().BeSameAs(EmployeeErrors.NameInvalidCharacter);
    }

    [Fact]
    public void MaxLength_Is100()
    {
        Name.MaxLength.Should().Be(100);
    }
}
