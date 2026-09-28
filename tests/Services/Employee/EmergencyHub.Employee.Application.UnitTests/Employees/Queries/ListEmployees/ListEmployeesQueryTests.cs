using EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Queries.ListEmployees;

// S07-T01(PRD-002 FR-07, ADR-0025): page · pageSize는 int?로 바인딩하고 빠진 값의 기본값(1 · 20)은 Query가 채운다. 범위 검사는 Validator 몫이라 여기서 거르지 않는다.
[Trait("FR", "PRD-002/FR-07")]
public sealed class ListEmployeesQueryTests
{
    // ---- 성공 ----

    [Fact]
    public void Create_BothMissing_UsesDefaultPageOneAndPageSizeTwenty()
    {
        var query = ListEmployeesQuery.Create(null, null);

        query.Should().Be(new ListEmployeesQuery(1, 20));
    }

    [Fact]
    public void Create_BothGiven_KeepsValues()
    {
        var query = ListEmployeesQuery.Create(2, 10);

        query.Page.Should().Be(2);
        query.PageSize.Should().Be(10);
    }

    [Fact]
    public void Constants_MatchFr07Ranges()
    {
        ListEmployeesQuery.DefaultPage.Should().Be(1);
        ListEmployeesQuery.MaxPage.Should().Be(100_000);
        ListEmployeesQuery.DefaultPageSize.Should().Be(20);
        ListEmployeesQuery.MinPageSize.Should().Be(1);
        ListEmployeesQuery.MaxPageSize.Should().Be(100);
    }

    // ---- 실패: 범위 밖 값도 그대로 넘겨 Validator가 1003으로 판정한다 ----

    [Theory]
    [InlineData(0, 101)]
    [InlineData(-1, 0)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void Create_OutOfRangeValues_AreKeptForValidator(int page, int pageSize)
    {
        var query = ListEmployeesQuery.Create(page, pageSize);

        query.Should().Be(new ListEmployeesQuery(page, pageSize));
    }

    // ---- 엣지: 하나만 빠짐 ----

    [Fact]
    public void Create_OnlyPageMissing_DefaultsPageOnly()
    {
        ListEmployeesQuery.Create(null, 50).Should().Be(new ListEmployeesQuery(1, 50));
    }

    [Fact]
    public void Create_OnlyPageSizeMissing_DefaultsPageSizeOnly()
    {
        ListEmployeesQuery.Create(7, null).Should().Be(new ListEmployeesQuery(7, 20));
    }
}
