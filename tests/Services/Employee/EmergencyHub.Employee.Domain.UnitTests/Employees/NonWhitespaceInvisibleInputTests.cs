using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// S05-T03 tester 보강(PRD-002 FR-01 "공백만" 엣지의 경계): 공백이 아니라 Trim · IsNullOrWhiteSpace에 걸리지 않는
// 보이지 않는 문자(U+0000 NUL, U+200B ZWSP)만 있거나 섞인 입력은 필수 오류가 아니라 각 필드의 문자 · 형식 오류다.
// Name의 같은 경계는 NameTests.Create_ControlCharacterOnlyThatIsNotWhitespace_ReturnsNameInvalidCharacter가 다룬다.
[Trait("FR", "PRD-002/FR-01")]
[Trait("NFR", "PRD-002/NFR-05")]
public sealed class NonWhitespaceInvisibleInputTests
{
    [Theory]
    [InlineData("\0")]
    [InlineData("​")]
    [InlineData("010-1234-5678\0")]
    [InlineData("​010-1234-5678")]
    public void PhoneNumber_InvisibleNonWhitespaceCharacter_ReturnsInvalidCharacterNotRequired(string input)
    {
        var result = PhoneNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.PhoneNumberInvalidCharacter);
    }

    [Theory]
    [InlineData("\0")]
    [InlineData("​")]
    [InlineData("2000-01-01\0")]
    [InlineData("​2000-01-01")]
    public void JoinedOn_InvisibleNonWhitespaceCharacter_ReturnsInvalidFormatNotRequired(string input)
    {
        var result = JoinedOn.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.JoinedOnInvalidFormat);
    }

    [Theory]
    [InlineData("\0")]
    [InlineData("​")]
    public void Email_InvisibleNonWhitespaceCharacterOnly_ReturnsInvalidNotRequired(string input)
    {
        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.EmailInvalid);
    }
}
