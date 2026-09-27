using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;
using EmergencyHub.Employee.Domain.Employees;
using FluentValidation.Results;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee;

// S03-T01 등록 Validator: 21001 ~ 21006(계획 리뷰 코드 선배정), employeeStatus 정의값은 공통 1002.
// 길이는 앞뒤 공백을 지운 뒤 string.Length로 잰다(Aggregate 정규화와 같은 기준이라 Validator 통과 뒤 도메인 예외가 나지 않는다).
// 한 속성의 규칙 체인은 첫 실패에서 멈추고 여러 속성의 실패는 모두 모은다(RequestValidator, ADR-0018).
public sealed class RegisterEmployeeCommandValidatorTests
{
    private const string EmailDomain = "@example.com";

    private readonly RegisterEmployeeCommandValidator _validator = new();

    public static TheoryData<string?> MissingValues() => new(null, string.Empty, " ", "\t\r\n");

    // ---- 성공 ----

    [Theory]
    [InlineData(EmployeeStatus.Active)]
    [InlineData(EmployeeStatus.Inactive)]
    public void Validate_ValidCommand_HasNoFailures(EmployeeStatus status)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithStatus(status).Build());

        result.IsValid.Should().BeTrue();
    }

    // ---- 실패: 규칙마다 ----

    [Theory]
    [MemberData(nameof(MissingValues))]
    public void Validate_MissingDisplayName_Reports21001Only(string? displayName)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithDisplayName(displayName!).Build());

        ShouldReportSingle(result, "DisplayName", EmployeeErrors.DisplayNameRequired);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(500)]
    public void Validate_DisplayNameOverMaxLength_Reports21002(int length)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithDisplayName(new string('a', length)).Build());

        ShouldReportSingle(result, "DisplayName", EmployeeErrors.DisplayNameTooLong);
    }

    [Theory]
    [MemberData(nameof(MissingValues))]
    public void Validate_MissingEmail_Reports21003Only(string? email)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithEmail(email!).Build());

        ShouldReportSingle(result, "Email", EmployeeErrors.EmailRequired);
    }

    [Theory]
    [InlineData("hong")]
    [InlineData("@example.com")]
    [InlineData("hong@")]
    [InlineData("a@b@example.com")]
    public void Validate_MalformedEmail_Reports21004(string email)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithEmail(email).Build());

        ShouldReportSingle(result, "Email", EmployeeErrors.EmailInvalid);
    }

    [Fact]
    public void Validate_EmailOverMaxLength_Reports21005()
    {
        var email = new string('a', 255 - EmailDomain.Length) + EmailDomain;

        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithEmail(email).Build());

        ShouldReportSingle(result, "Email", EmployeeErrors.EmailTooLong);
    }

    [Fact]
    public void Validate_MissingEmployeeStatus_Reports21006Only()
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithStatus(null).Build());

        ShouldReportSingle(result, "EmployeeStatus", EmployeeErrors.EmployeeStatusRequired);
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)3)]
    [InlineData((short)99)]
    [InlineData((short)-1)]
    public void Validate_ReservedOrUndefinedEmployeeStatus_Reports1002OnEmployeeStatus(short rawStatus)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithStatus((EmployeeStatus)rawStatus).Build());

        ShouldReportSingle(result, "EmployeeStatus", CommonErrors.InvalidCode);
    }

    // ---- 엣지 ----

    [Theory]
    [MemberData(nameof(DisplayNamesWithinMaxLength))]
    public void Validate_DisplayNameWithinMaxLengthAfterTrim_HasNoFailures(string displayName)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithDisplayName(displayName).Build());

        result.IsValid.Should().BeTrue();
    }

    public static TheoryData<string> DisplayNamesWithinMaxLength() => new(
        "가",
        new string('a', 100),
        new string('가', 100),
        string.Concat(Enumerable.Repeat("😀", 50)),
        "   " + new string('가', 100) + "   ");

    [Theory]
    [MemberData(nameof(DisplayNamesOverMaxLength))]
    public void Validate_UnicodeDisplayNameOverMaxLength_Reports21002(string displayName)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithDisplayName(displayName).Build());

        ShouldReportSingle(result, "DisplayName", EmployeeErrors.DisplayNameTooLong);
    }

    public static TheoryData<string> DisplayNamesOverMaxLength() => new(
        new string('가', 101),
        "a" + string.Concat(Enumerable.Repeat("😀", 50)),
        " " + new string('가', 101) + " ");

    [Theory]
    [InlineData("Hong@Example.COM")]
    [InlineData("  hong@example.com  ")]
    [InlineData("İstanbul@example.com")]
    public void Validate_EmailWithCaseOrSurroundingWhitespace_HasNoFailures(string email)
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithEmail(email).Build());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmailAtMaxLengthWithSurroundingWhitespace_HasNoFailures()
    {
        var email = "  " + new string('a', 254 - EmailDomain.Length) + EmailDomain + "  ";

        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithEmail(email).Build());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_LongMalformedEmail_ReportsLengthFirstAndStops()
    {
        var result = _validator.Validate(new RegisterEmployeeCommandBuilder().WithEmail(new string('a', 300)).Build());

        ShouldReportSingle(result, "Email", EmployeeErrors.EmailTooLong);
    }

    [Fact]
    public void Validate_AllPropertiesInvalid_CollectsOneFailurePerPropertyInOrder()
    {
        var command = new RegisterEmployeeCommand(string.Empty, "hong", null);

        var result = _validator.Validate(command);

        result.Errors.Select(failure => (failure.PropertyName, ((Error)failure.CustomState).Code)).Should().Equal(
            ("DisplayName", 21001),
            ("Email", 21004),
            ("EmployeeStatus", 21006));
    }

    private static void ShouldReportSingle(ValidationResult result, string propertyName, Error expected)
    {
        var failure = result.Errors.Should().ContainSingle().Subject;
        failure.PropertyName.Should().Be(propertyName);
        failure.CustomState.Should().BeSameAs(expected);
        failure.ErrorMessage.Should().Be(expected.Message);
    }
}
