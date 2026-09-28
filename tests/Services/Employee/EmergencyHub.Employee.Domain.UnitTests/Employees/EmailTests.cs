using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// S05-T03 Email Value Object(PRD-002 FR-01, ADR-0026 8절 · ADR-0027): Trim → 필수(21003) → 길이 254(21005) → 형식(21004, S06-T04부터 Cc 제어 문자 · 짝 없는 서로게이트 포함, BL-129).
// Value는 Trim만 한 입력 표기, NormalizedEmail은 Value.ToLowerInvariant()(NFC 없음)다.
[Trait("FR", "PRD-002/FR-01")]
[Trait("NFR", "PRD-002/NFR-05")]
public sealed class EmailTests
{
    private const string ExampleDomain = "@example.com";

    public static TheoryData<string?> BlankValues() => new(null, string.Empty, " ", "   ", "\t", "\r\n", "　");

    public static TheoryData<string> MalformedEmails() => new(
        "hong",
        "@example.com",
        "hong@",
        "@",
        "hong@@example.com",
        "a@b@example.com",
        "hong@example",
        "hong gil@example.com",
        "hong@exa mple.com",
        "hong\t@example.com",
        "hong@example　.com",
        "a@.com",
        "a@com.",
        "a@b..c",
        "a@.",
        "a@..",
        "a@b.c.");

    // ---- 성공 ----

    [Theory]
    [InlineData("hong@example.com", "hong@example.com", "hong@example.com")]
    [InlineData("  Hong@Example.COM\t", "Hong@Example.COM", "hong@example.com")]
    [InlineData("a@b.c", "a@b.c", "a@b.c")]
    [InlineData("hong.gil-dong+tag@mail.example.co.kr", "hong.gil-dong+tag@mail.example.co.kr", "hong.gil-dong+tag@mail.example.co.kr")]
    [InlineData("홍길동@example.com", "홍길동@example.com", "홍길동@example.com")]
    public void Create_ValidEmail_KeepsTrimmedInputAndLowercasesNormalized(string input, string value, string normalized)
    {
        var result = Email.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
        result.Value.NormalizedEmail.Should().Be(normalized);
    }

    [Fact]
    public void Create_EmailsDifferingOnlyInCase_HaveSameNormalizedEmail()
    {
        var upper = Email.Create("Hong@Example.COM").Value;
        var lower = Email.Create("hong@example.com").Value;

        upper.NormalizedEmail.Should().Be(lower.NormalizedEmail);
        upper.Value.Should().NotBe(lower.Value);
        upper.Should().NotBe(lower);
    }

    [Fact]
    public void Create_SameInputAfterTrim_AreEqual()
    {
        Email.Create(" hong@example.com ").Value.Should().Be(Email.Create("hong@example.com").Value);
    }

    [Fact]
    public void Create_AtMaxLengthAfterTrim_Succeeds()
    {
        var input = "  " + new string('a', Email.MaxLength - ExampleDomain.Length) + ExampleDomain + "  ";

        var result = Email.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Length.Should().Be(Email.MaxLength);
        result.Value.NormalizedEmail.Length.Should().Be(Email.MaxLength);
    }

    [Fact]
    public void Create_DottedLocalPart_IsNotCheckedForDots()
    {
        // 첫 · 끝 · 연속 `.` 거부는 domain 규칙이다(error-codes 21004). local 부분은 `@` · 공백 규칙만 적용한다.
        var result = Email.Create(".hong..gil.@example.com");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_TurkishCapitalIWithDot_IsNotChangedByInvariantLowercase()
    {
        // ToLowerInvariant는 U+0130을 바꾸지 않는다(ADR-0027 비ASCII 한계, 문화권 무관).
        var result = Email.Create("İstanbul@Example.com");

        result.Value.NormalizedEmail.Should().Be("İstanbul@example.com");
    }

    [Fact]
    public void Create_NfdEmail_IsNotNfcNormalized()
    {
        // 이메일은 NFC 정규화를 하지 않는다(ADR-0027). NFD와 NFC는 다른 값이다.
        var nfd = Email.Create("josé@example.com").Value;
        var nfc = Email.Create("josé@example.com").Value;

        nfd.Value.Should().Be("josé@example.com");
        nfd.NormalizedEmail.Should().NotBe(nfc.NormalizedEmail);
    }

    // ---- 실패: 필수(21003) ----

    [Theory]
    [MemberData(nameof(BlankValues))]
    public void Create_NullEmptyOrWhitespace_ReturnsEmailRequired(string? input)
    {
        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.EmailRequired);
    }

    // ---- 실패: 길이(21005) ----

    [Fact]
    public void Create_OverMaxLengthAfterTrim_ReturnsEmailTooLong()
    {
        var input = new string('a', Email.MaxLength - ExampleDomain.Length + 1) + ExampleDomain;

        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.EmailTooLong);
    }

    [Fact]
    public void Create_LongMalformedEmail_ReportsLengthFirst()
    {
        var result = Email.Create(new string('a', Email.MaxLength + 1));

        result.Error.Should().BeSameAs(EmployeeErrors.EmailTooLong);
    }

    // ---- 실패: 형식(21004) ----

    [Theory]
    [MemberData(nameof(MalformedEmails))]
    public void Create_Malformed_ReturnsEmailInvalid(string input)
    {
        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.EmailInvalid);
    }

    // ---- 실패: 제어 문자 · 짝 없는 서로게이트(21004, BL-129, PRD-002 FR-01) ----

    public static TheoryData<string> EmailsWithControlCharacter() => new(
        "hong\u0000@example.com",
        "hong\u0001@example.com",
        "ho\tng@example.com",
        "hong@exam\u007Fple.com",
        "hong@example.com\u001F",
        "hong\u0085@example.com",
        "hong\u009F@example.com",
        "\u0001hong@example.com");

    public static TheoryData<string> EmailsWithUnpairedSurrogate() => new(
        "hong\uD83D@example.com",
        "hong\uDE00@example.com",
        "hong@example.com\uD83D",
        "\uDE00hong@example.com",
        "hong\uDE00\uD83D@example.com");

    [Theory]
    // 제어 문자는 발견 단계 직렬화에서 값이 바뀔 수 있어 발견 단계 열거를 끄고 입력 보존을 단언한다(BL-131).
    [MemberData(nameof(EmailsWithControlCharacter), DisableDiscoveryEnumeration = true)]
    public void Create_ControlCharacterNotTrimmed_ReturnsEmailInvalid(string input)
    {
        input.Trim().Any(char.IsControl).Should().BeTrue("테스트 데이터가 직렬화로 바뀌지 않고 Trim 뒤에도 제어 문자를 담아야 한다");

        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.EmailInvalid);
    }

    [Theory]
    [MemberData(nameof(EmailsWithUnpairedSurrogate), DisableDiscoveryEnumeration = true)]
    public void Create_UnpairedSurrogate_ReturnsEmailInvalidWithoutThrowing(string input)
    {
        input.Any(char.IsSurrogate).Should().BeTrue("테스트 데이터가 직렬화로 바뀌지 않고 서로게이트를 담아야 한다");
        var act = () => Email.Create(input);

        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.EmailInvalid);
    }

    [Fact]
    public void Create_PairedSurrogate_IsAllowed()
    {
        var result = Email.Create("hong😀@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("hong😀@example.com");
    }

    [Fact]
    public void Create_ControlWhitespaceOnlyAtEdges_IsTrimmedAndSucceeds()
    {
        // 탭 · 줄바꿈은 공백이기도 해서 앞뒤에 있으면 Trim으로 지워진다(Name과 같은 경계). 안쪽에 남은 제어 문자만 거부한다.
        var result = Email.Create("\t\nhong@example.com\n\t");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("hong@example.com");
    }

    [Fact]
    public void Create_FormatCharacterInside_IsNotRejectedByControlRule()
    {
        // 서식 문자(Cf, U+200D ZWJ)는 Cc가 아니므로 이 규칙으로 거부하지 않는다(FR-01은 Cc만, Name 21009와 같은 범위).
        var result = Email.Create("hong‍@example.com");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_OverMaxLengthWithControlCharacter_ReportsLengthFirst()
    {
        // 판정 순서는 필수 → 길이 → 형식이다. 제어 문자는 형식(21004) 판정이라 길이 초과(21005)가 먼저다.
        var input = "\u0001" + new string('a', Email.MaxLength - ExampleDomain.Length) + ExampleDomain;

        var result = Email.Create(input);

        result.Error.Should().BeSameAs(EmployeeErrors.EmailTooLong);
    }

    [Fact]
    public void MaxLength_Is254()
    {
        Email.MaxLength.Should().Be(254);
    }
}
