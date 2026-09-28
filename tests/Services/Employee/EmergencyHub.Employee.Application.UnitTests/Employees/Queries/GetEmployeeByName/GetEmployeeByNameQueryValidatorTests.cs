using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;
using EmergencyHub.Employee.Domain.Employees;
using FluentValidation.Results;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Queries.GetEmployeeByName;

// S07-T02(PRD-002 FR-08): 경로 {name}의 판정 원본은 Name Value Object(Name.Create)다. 공백을 뗀 뒤 빈 이름은 21007,
// 제어 문자 · 짝 없는 서로게이트는 21009, NFC 뒤 100자 초과는 21008이고, 검증 데코레이터가 ValidationError(대표 1001, errors.name)로 담는다.
[Trait("FR", "PRD-002/FR-08")]
[Trait("FR", "PRD-002/FR-10")]
public sealed class GetEmployeeByNameQueryValidatorTests
{
    private readonly GetEmployeeByNameQueryValidator _validator = new();

    public static TheoryData<string?> BlankNames() => new(null, string.Empty, " ", "   ", "\t", "　", " \r\n ");

    public static TheoryData<string> InvalidCharacterNames() => new("홍\u0001길동", "홍길동\u007F", "홍\u0085길동", "홍\uD800길동", "홍길동\uDC00");

    // ---- 성공 ----

    [Theory]
    [InlineData("홍길동")]
    [InlineData("Hong Gildong")]
    [InlineData("  홍길동  ")]
    [InlineData("홍길동 😀")]
    [InlineData("홍‍길동")]
    public void Validate_ValidName_IsValid(string name)
    {
        _validator.Validate(new GetEmployeeByNameQuery(name)).IsValid.Should().BeTrue();
    }

    // ---- 실패 ----

    [Theory]
    [MemberData(nameof(BlankNames))]
    public void Validate_BlankAfterTrim_Reports21007(string? name)
    {
        AssertSingleFailure(_validator.Validate(new GetEmployeeByNameQuery(name)), EmployeeErrors.NameRequired, 21007);
    }

    [Theory]
    [MemberData(nameof(InvalidCharacterNames), DisableDiscoveryEnumeration = true)]
    public void Validate_ControlOrUnpairedSurrogate_Reports21009(string name)
    {
        // 짝 없는 서로게이트는 발견 단계 직렬화에서 바뀔 수 있어 발견 열거를 끈다(NameTests와 같은 방식).
        (name.Any(char.IsControl) || name.Any(char.IsSurrogate)).Should().BeTrue("테스트 데이터가 직렬화로 바뀌지 않아야 한다");
        AssertSingleFailure(_validator.Validate(new GetEmployeeByNameQuery(name)), EmployeeErrors.NameInvalidCharacter, 21009);
    }

    [Fact]
    public void Validate_LongerThan100AfterNfc_Reports21008()
    {
        AssertSingleFailure(_validator.Validate(new GetEmployeeByNameQuery(new string('가', Name.MaxLength + 1))), EmployeeErrors.NameTooLong, 21008);
    }

    // ---- 엣지 ----

    [Fact]
    public void Validate_Exactly100Characters_IsValid()
    {
        _validator.Validate(new GetEmployeeByNameQuery(new string('가', Name.MaxLength))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NfdLongerThan100ButNfcIs100_IsValid()
    {
        // NFD '가' = U+1100 U+1161(2자). 100자 NFC 이름의 NFD는 200자지만 길이는 NFC 뒤에 잰다(Name.Create).
        var nfd = new string('가', Name.MaxLength).Normalize(System.Text.NormalizationForm.FormD);
        nfd.Length.Should().Be(Name.MaxLength * 2);

        _validator.Validate(new GetEmployeeByNameQuery(nfd)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_SurroundingSpacesMakeItLongerThan100_IsValid()
    {
        // 앞뒤 공백은 길이에 들어가지 않는다(Trim 뒤 판정).
        _validator.Validate(new GetEmployeeByNameQuery("  " + new string('가', Name.MaxLength) + "  ")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidCharacterAndTooLong_ReportsOnlyFirstRuleOfName()
    {
        // Name.Create의 판정 순서(21007 → 21009 → 21008)대로 첫 실패 하나만 보고한다.
        var name = "\u0001" + new string('가', Name.MaxLength + 1);

        AssertSingleFailure(_validator.Validate(new GetEmployeeByNameQuery(name)), EmployeeErrors.NameInvalidCharacter, 21009);
    }

    [Fact]
    public void Validate_FailureMessage_IsFixedTextWithoutInputValue()
    {
        // NFR-04: 에러 메시지(detail · errors[].message)에 이름 값이 들어가지 않는다.
        var result = _validator.Validate(new GetEmployeeByNameQuery("홍길동\u0001"));

        var failure = result.Errors.Should().ContainSingle().Subject;
        failure.ErrorMessage.Should().Be(EmployeeErrors.NameInvalidCharacter.Message);
        failure.ErrorMessage.Should().NotContain("홍길동");
        failure.AttemptedValue.Should().BeNull("입력 값을 실패에 싣지 않는다");
    }

    private static void AssertSingleFailure(ValidationResult result, Error expected, int code)
    {
        result.IsValid.Should().BeFalse();
        var failure = result.Errors.Should().ContainSingle().Subject;
        failure.PropertyName.Should().Be(nameof(GetEmployeeByNameQuery.Name));
        failure.CustomState.Should().BeSameAs(expected);
        ((Error)failure.CustomState).Code.Should().Be(code);
        failure.ErrorMessage.Should().Be(expected.Message);
    }
}
