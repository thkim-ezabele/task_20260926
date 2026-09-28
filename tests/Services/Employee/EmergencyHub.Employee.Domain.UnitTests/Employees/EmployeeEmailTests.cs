using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// 이메일 정규화 · 형식 판정의 원본. Aggregate 불변식과 Application Validator(21004 · 21005)가 같은 판정을 쓴다.
public sealed class EmployeeEmailTests
{
    // ---- Normalize ----

    [Theory]
    [InlineData("Hong@Example.COM", "hong@example.com")]
    [InlineData("  hong@example.com\t", "hong@example.com")]
    [InlineData("hong@example.com", "hong@example.com")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Normalize_TrimsAndLowercasesInvariant(string input, string expected)
    {
        EmployeeEmail.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_Null_ThrowsArgumentNullException()
    {
        var act = () => EmployeeEmail.Normalize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Normalize_AlreadyNormalized_IsIdempotent()
    {
        var once = EmployeeEmail.Normalize(" İstanbul@Example.com ");

        EmployeeEmail.Normalize(once).Should().Be(once);
    }

    // ---- IsWellFormed ----

    [Theory]
    [InlineData("a@b")]
    [InlineData("hong@example.com")]
    [InlineData("홍길동@example.com")]
    public void IsWellFormed_SingleAtBetweenNonEmptyParts_ReturnsTrue(string email)
    {
        EmployeeEmail.IsWellFormed(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("hong")]
    [InlineData("@example.com")]
    [InlineData("hong@")]
    [InlineData("@")]
    [InlineData("hong@@example.com")]
    [InlineData("a@b@example.com")]
    public void IsWellFormed_MissingOrMultipleAtOrEmptyPart_ReturnsFalse(string email)
    {
        EmployeeEmail.IsWellFormed(email).Should().BeFalse();
    }

    [Fact]
    public void IsWellFormed_Null_ThrowsArgumentNullException()
    {
        var act = () => EmployeeEmail.IsWellFormed(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
