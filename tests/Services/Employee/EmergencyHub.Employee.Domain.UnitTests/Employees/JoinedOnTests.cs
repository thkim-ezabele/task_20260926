using System.Globalization;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// S05-T03 JoinedOn Value Object(PRD-002 FR-01, ADR-0026 8절): 필수(21015) → `yyyy-MM-dd` 정확 파싱(21016, InvariantCulture) → 하한 1900-01-01(21017).
// 미래 날짜는 허용하므로 현재 시각에 의존하지 않는다(TimeProvider 불필요).
[Trait("FR", "PRD-002/FR-01")]
[Trait("NFR", "PRD-002/NFR-05")]
public sealed class JoinedOnTests
{
    public static TheoryData<string?> BlankValues() => new(null, string.Empty, " ", "   ", "\t", "\r\n", "　");

    // ---- 성공 ----

    [Theory]
    [InlineData("2000-01-01", 2000, 1, 1)]
    [InlineData("2024-12-31", 2024, 12, 31)]
    [InlineData("2000-02-29", 2000, 2, 29)]
    [InlineData("2024-02-29", 2024, 2, 29)]
    public void Create_ValidDate_ReturnsDate(string input, int year, int month, int day)
    {
        var result = JoinedOn.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    public void Create_MinValue_Succeeds()
    {
        var result = JoinedOn.Create("1900-01-01");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(JoinedOn.MinValue);
    }

    [Theory]
    [InlineData("2999-01-01")]
    [InlineData("9999-12-31")]
    public void Create_FutureDate_IsAllowed(string input)
    {
        JoinedOn.Create(input).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_UnderNonInvariantCurrentCulture_ParsesSameDate()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // th-TH는 불기(佛紀) 달력을 쓴다. 현재 문화권을 따르면 연도가 543년 어긋난다.
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");

            JoinedOn.Create("2000-01-01").Value.Value.Should().Be(new DateOnly(2000, 1, 1));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Create_SameDate_AreEqual()
    {
        JoinedOn.Create("2000-01-01").Value.Should().Be(JoinedOn.Create("2000-01-01").Value);
    }

    // ---- 실패: 필수(21015) ----

    [Theory]
    [MemberData(nameof(BlankValues))]
    public void Create_NullEmptyOrWhitespace_ReturnsJoinedOnRequired(string? input)
    {
        var result = JoinedOn.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.JoinedOnRequired);
    }

    // ---- 실패: 형식 · 없는 날짜(21016) ----

    [Theory]
    [InlineData("2000-2-3")]
    [InlineData("2000-02-3")]
    [InlineData("2000-2-03")]
    [InlineData("2000-02-30")]
    [InlineData("2001-02-29")]
    [InlineData("1900-02-29")]
    [InlineData("2000-13-01")]
    [InlineData("2000-00-01")]
    [InlineData("2000-01-00")]
    [InlineData("2000-04-31")]
    [InlineData("20000101")]
    [InlineData("2000/01/01")]
    [InlineData("2000.01.01")]
    [InlineData("01-01-2000")]
    [InlineData("00-01-01")]
    [InlineData(" 2000-01-01")]
    [InlineData("2000-01-01 ")]
    [InlineData("2000-01-01T00:00:00")]
    [InlineData("10000-01-01")]
    [InlineData("-2000-01-01")]
    [InlineData("+2000-01-01")]
    [InlineData("２０００-０１-０１")]
    [InlineData("abcd-ef-gh")]
    public void Create_NotExactFormatOrNonexistentDate_ReturnsJoinedOnInvalidFormat(string input)
    {
        var result = JoinedOn.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.JoinedOnInvalidFormat);
    }

    // ---- 실패: 하한(21017) ----

    [Theory]
    [InlineData("1899-12-31")]
    [InlineData("1800-06-15")]
    [InlineData("0001-01-01")]
    public void Create_BeforeMinValue_ReturnsJoinedOnTooEarly(string input)
    {
        var result = JoinedOn.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.JoinedOnTooEarly);
    }

    [Fact]
    public void Constants_MatchFieldRules()
    {
        JoinedOn.Format.Should().Be("yyyy-MM-dd");
        JoinedOn.MinValue.Should().Be(new DateOnly(1900, 1, 1));
    }
}
