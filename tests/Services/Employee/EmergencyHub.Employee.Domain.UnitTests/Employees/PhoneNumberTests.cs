using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// S05-T03 PhoneNumber Value Object(PRD-002 FR-01, ADR-0026 8절): 입력 그대로 보존(Trim 없음).
// 판정 순서: 필수(21010) → 허용 문자(21011, ASCII 숫자와 `-`만) → 전체 길이 20(21013) → 하이픈 위치(21014) → 숫자 8 ~ 15자리(21012).
[Trait("FR", "PRD-002/FR-01")]
[Trait("NFR", "PRD-002/NFR-05")]
public sealed class PhoneNumberTests
{
    public static TheoryData<string?> BlankValues() => new(null, string.Empty, " ", "   ", "\t", "\r\n", "　");

    public static TheoryData<string> InvalidCharacterValues() => new(
        "+82-10-1234-5678",
        "+821012345678",
        "010 1234 5678",
        " 010-1234-5678",
        "010-1234-5678 ",
        "010.1234.5678",
        "(010)12345678",
        "010-1234-567a",
        "０１０-1234-5678",
        "٠١٠١٢٣٤٥٦٧٨",
        "010–1234–5678",
        "010-1234-5678\n");

    // ---- 성공 ----

    [Theory]
    [InlineData("010-1234-5678")]
    [InlineData("01012345678")]
    [InlineData("02-123-4567")]
    [InlineData("1588-1234")]
    [InlineData("12345678")]
    [InlineData("123456789012345")]
    [InlineData("12-34-56-78-90-12345")]
    [InlineData("1-2-3-4-5-6-7-8")]
    public void Create_ValidPhoneNumber_KeepsInputAsIs(string input)
    {
        var result = PhoneNumber.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(input);
    }

    [Fact]
    public void Create_AtMaxLengthWithMaxDigits_Succeeds()
    {
        var input = "12-34-56-78-90-12345";

        input.Length.Should().Be(PhoneNumber.MaxLength);
        input.Count(char.IsAsciiDigit).Should().Be(PhoneNumber.MaxDigitCount);
        PhoneNumber.Create(input).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_SameInput_AreEqual()
    {
        PhoneNumber.Create("010-1234-5678").Value.Should().Be(PhoneNumber.Create("010-1234-5678").Value);
    }

    [Fact]
    public void Create_SameDigitsDifferentHyphens_AreNotEqual()
    {
        // 입력 그대로 보존하므로 하이픈 위치가 다르면 다른 값이다(정규화 · 중복 판정 없음, PRD FR-01).
        PhoneNumber.Create("010-1234-5678").Value.Should().NotBe(PhoneNumber.Create("01012345678").Value);
    }

    // ---- 실패: 필수(21010) ----

    [Theory]
    [MemberData(nameof(BlankValues))]
    public void Create_NullEmptyOrWhitespace_ReturnsPhoneNumberRequired(string? input)
    {
        var result = PhoneNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.PhoneNumberRequired);
    }

    // ---- 실패: 허용 문자(21011) ----

    [Theory]
    [MemberData(nameof(InvalidCharacterValues))]
    public void Create_CharacterOtherThanAsciiDigitOrHyphen_ReturnsPhoneNumberInvalidCharacter(string input)
    {
        var result = PhoneNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.PhoneNumberInvalidCharacter);
    }

    // ---- 실패: 숫자 자리 수(21012) ----

    [Theory]
    [InlineData("1234567")]
    [InlineData("123-4567")]
    [InlineData("1234567890123456")]
    [InlineData("1234-5678-9012-3456")]
    [InlineData("1")]
    public void Create_DigitCountOutOfRange_ReturnsPhoneNumberDigitCountOutOfRange(string input)
    {
        var result = PhoneNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.PhoneNumberDigitCountOutOfRange);
    }

    // ---- 실패: 전체 길이(21013) ----

    [Theory]
    [InlineData("1-2-3-4-5-6-789012345")]
    [InlineData("123456789012345678901")]
    public void Create_OverMaxLength_ReturnsPhoneNumberTooLong(string input)
    {
        input.Length.Should().Be(PhoneNumber.MaxLength + 1);

        var result = PhoneNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.PhoneNumberTooLong);
    }

    // ---- 실패: 하이픈 위치(21014) ----

    [Theory]
    [InlineData("-010-1234-5678")]
    [InlineData("010-1234-5678-")]
    [InlineData("010--1234-5678")]
    [InlineData("-01012345678")]
    [InlineData("01012345678-")]
    [InlineData("0101234---5678")]
    [InlineData("-")]
    [InlineData("--")]
    public void Create_LeadingTrailingOrConsecutiveHyphen_ReturnsPhoneNumberInvalidHyphen(string input)
    {
        var result = PhoneNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.PhoneNumberInvalidHyphen);
    }

    // ---- 엣지: 판정 순서 ----

    [Fact]
    public void Create_OverMaxLengthWithPlus_ReportsInvalidCharacterFirst()
    {
        PhoneNumber.Create("+82-10-1234-5678-9999").Error.Should().BeSameAs(EmployeeErrors.PhoneNumberInvalidCharacter);
    }

    [Fact]
    public void Create_OverMaxLengthWithLeadingHyphen_ReportsTooLongFirst()
    {
        PhoneNumber.Create("-12345678901234567890").Error.Should().BeSameAs(EmployeeErrors.PhoneNumberTooLong);
    }

    [Fact]
    public void Create_TooFewDigitsWithLeadingHyphen_ReportsInvalidHyphenFirst()
    {
        PhoneNumber.Create("-123").Error.Should().BeSameAs(EmployeeErrors.PhoneNumberInvalidHyphen);
    }

    [Fact]
    public void Constants_MatchFieldRules()
    {
        PhoneNumber.MaxLength.Should().Be(20);
        PhoneNumber.MinDigitCount.Should().Be(8);
        PhoneNumber.MaxDigitCount.Should().Be(15);
    }
}
