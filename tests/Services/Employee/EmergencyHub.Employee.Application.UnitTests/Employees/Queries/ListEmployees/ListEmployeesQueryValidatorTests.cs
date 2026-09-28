using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;
using FluentValidation.Results;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Queries.ListEmployees;

// S07-T01(PRD-002 FR-07, ADR-0025): page 1 ~ 100,000, pageSize 1 ~ 100. 범위 밖은 필드마다 1003(Common.InvalidPaging)이고,
// 검증 데코레이터가 ValidationError(대표 1001, errors.page · errors.pageSize의 code 1003)로 담는다. 숫자가 아닌 값(1001)은 바인딩 단계라 여기 오지 않는다.
[Trait("FR", "PRD-002/FR-07")]
[Trait("FR", "PRD-002/FR-10")]
public sealed class ListEmployeesQueryValidatorTests
{
    private readonly ListEmployeesQueryValidator _validator = new();

    public static TheoryData<int> ValidPages() => new(1, 2, 99_999, 100_000);

    public static TheoryData<int> ValidPageSizes() => new(1, 2, 20, 99, 100);

    public static TheoryData<int> InvalidPages() => new(0, -1, 100_001, int.MinValue, int.MaxValue);

    public static TheoryData<int> InvalidPageSizes() => new(0, -1, 101, int.MinValue, int.MaxValue);

    // ---- 성공 ----

    [Fact]
    public void Validate_Defaults_IsValid()
    {
        _validator.Validate(ListEmployeesQuery.Create(null, null)).IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ValidPages))]
    public void Validate_PageInRange_IsValid(int page)
    {
        _validator.Validate(new ListEmployeesQuery(page, 20)).IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ValidPageSizes))]
    public void Validate_PageSizeInRange_IsValid(int pageSize)
    {
        _validator.Validate(new ListEmployeesQuery(1, pageSize)).IsValid.Should().BeTrue();
    }

    // ---- 실패 ----

    [Theory]
    [MemberData(nameof(InvalidPages))]
    public void Validate_PageOutOfRange_ReportsInvalidPagingOnPage(int page)
    {
        var result = _validator.Validate(new ListEmployeesQuery(page, 20));

        AssertSingleInvalidPaging(result, nameof(ListEmployeesQuery.Page));
    }

    [Theory]
    [MemberData(nameof(InvalidPageSizes))]
    public void Validate_PageSizeOutOfRange_ReportsInvalidPagingOnPageSize(int pageSize)
    {
        var result = _validator.Validate(new ListEmployeesQuery(1, pageSize));

        AssertSingleInvalidPaging(result, nameof(ListEmployeesQuery.PageSize));
    }

    [Fact]
    public void Validate_BothOutOfRange_ReportsBothInDeclarationOrder()
    {
        var result = _validator.Validate(new ListEmployeesQuery(0, 101));

        result.Errors.Select(failure => failure.PropertyName).Should().Equal(nameof(ListEmployeesQuery.Page), nameof(ListEmployeesQuery.PageSize));
        result.Errors.Select(failure => failure.CustomState).Should().AllSatisfy(state => state.Should().BeSameAs(CommonErrors.InvalidPaging));
    }

    // ---- 엣지 ----

    [Fact]
    public void Validate_FailureMessage_IsFixedTextWithoutInputValue()
    {
        // InclusiveBetween 기본 메시지는 {PropertyValue}를 담는다. WithError가 고정 문구로 바꿔야 한다.
        var result = _validator.Validate(new ListEmployeesQuery(123_456_789, 20));

        var failure = result.Errors.Should().ContainSingle().Subject;
        failure.ErrorMessage.Should().Be(CommonErrors.InvalidPaging.Message);
        failure.ErrorMessage.Should().NotContain("123456789").And.NotContain("123,456,789");
    }

    [Fact]
    public void Validate_LargestValidPageAndPageSize_IsValid()
    {
        _validator.Validate(new ListEmployeesQuery(ListEmployeesQuery.MaxPage, ListEmployeesQuery.MaxPageSize)).IsValid.Should().BeTrue();
    }

    private static void AssertSingleInvalidPaging(ValidationResult result, string propertyName)
    {
        result.IsValid.Should().BeFalse();
        var failure = result.Errors.Should().ContainSingle().Subject;
        failure.PropertyName.Should().Be(propertyName);
        failure.CustomState.Should().BeSameAs(CommonErrors.InvalidPaging);
        ((Error)failure.CustomState).Code.Should().Be(1003);
    }
}
