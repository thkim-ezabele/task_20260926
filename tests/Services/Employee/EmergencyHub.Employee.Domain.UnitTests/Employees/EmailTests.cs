using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// S05-T03 Email Value Object(PRD-002 FR-01, ADR-0026 8절 · ADR-0027): Trim → 필수(21003) → 길이 254(21005) → 형식(21004).
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

    [Fact]
    public void MaxLength_Is254()
    {
        Email.MaxLength.Should().Be(254);
    }
}
