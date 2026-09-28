using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Queries.ListEmployees;

// S07-T01(PRD-002 FR-07): Read Repository의 목록(skip · take)과 개수를 따로 조회해 Handler가 { items, totalCount, page, pageSize }로 합친다(COUNT(*) OVER() 미사용).
// skip = (page - 1) * pageSize(checked). 정렬(joined_on → id)은 Repository 쿼리 몫이라 Handler는 받은 순서를 그대로 둔다.
[Trait("FR", "PRD-002/FR-07")]
[Trait("FR", "PRD-002/FR-10")]
public sealed class ListEmployeesQueryHandlerTests
{
    private readonly IEmployeeReadRepository _repository = Substitute.For<IEmployeeReadRepository>();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // ---- 성공 ----

    [Fact]
    public async Task Handle_SecondPageOfTwentyFive_ReadsSkipTenTakeTenAndReturnsItemsWithTotal()
    {
        var stored = Contacts(25);
        Arrange(stored, skip: 10, take: 10);

        var result = await CreateHandler().Handle(new ListEmployeesQuery(2, 10), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(item => item.Id).Should().Equal(stored.Skip(10).Take(10).Select(contact => contact.Id), "11 ~ 20번째");
        result.Value.TotalCount.Should().Be(25);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_Items_MapContactFieldsToTelAndJoined()
    {
        var contact = new EmployeeContactResponse(Guid.NewGuid(), "홍길동", "Hong@Example.com", "010-1234-5678", new DateOnly(2020, 1, 2));
        Arrange([contact], skip: 0, take: 20);

        var result = await CreateHandler().Handle(ListEmployeesQuery.Create(null, null), CancellationToken);

        result.Value.Items.Should().Equal(new EmployeeResponse(contact.Id, "홍길동", "Hong@Example.com", "010-1234-5678", new DateOnly(2020, 1, 2)));
    }

    [Fact]
    public async Task Handle_ListAndCount_AreSeparateCallsWithSameToken()
    {
        using var cancellation = new CancellationTokenSource();
        Arrange(Contacts(3), skip: 0, take: 20);

        await CreateHandler().Handle(new ListEmployeesQuery(1, 20), cancellation.Token);

        await _repository.Received(1).ListOrderedByJoinedOnAsync(0, 20, cancellation.Token);
        await _repository.Received(1).CountAsync(cancellation.Token);
        await _repository.DidNotReceiveWithAnyArgs().FindFirstByNameAsync(default!, CancellationToken);
    }

    // ---- 실패: Repository 예외는 예상하지 못한 오류라 Result로 바꾸지 않는다 ----

    [Fact]
    public async Task Handle_ListThrows_PropagatesException()
    {
        _repository.ListOrderedByJoinedOnAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<List<EmployeeContactResponse>>>(_ => throw new InvalidOperationException("db"));

        var act = () => CreateHandler().Handle(new ListEmployeesQuery(1, 20), CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_CountThrows_PropagatesException()
    {
        _repository.ListOrderedByJoinedOnAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _repository.CountAsync(Arg.Any<CancellationToken>()).Returns<Task<int>>(_ => throw new OperationCanceledException());

        var act = () => CreateHandler().Handle(new ListEmployeesQuery(1, 20), CancellationToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Handle_SkipOverflowsInt_ThrowsOverflowWithoutQuerying()
    {
        // Validator를 거치지 않은 값: (int.MaxValue - 1) * 100은 int를 넘는다. checked 계산이라 음수 skip으로 조회하지 않는다.
        var act = () => CreateHandler().Handle(new ListEmployeesQuery(int.MaxValue, 100), CancellationToken);

        await act.Should().ThrowAsync<OverflowException>();
        await _repository.DidNotReceiveWithAnyArgs().ListOrderedByJoinedOnAsync(default, default, CancellationToken);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Handle_PageBeyondLast_ReturnsEmptyItemsWithCorrectTotal()
    {
        Arrange(Contacts(25), skip: 30, take: 10);

        var result = await CreateHandler().Handle(new ListEmployeesQuery(4, 10), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(25);
        result.Value.Page.Should().Be(4);
    }

    [Fact]
    public async Task Handle_NoEmployees_ReturnsEmptyItemsAndZeroTotal()
    {
        Arrange([], skip: 0, take: 20);

        var result = await CreateHandler().Handle(new ListEmployeesQuery(1, 20), CancellationToken);

        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_LastFullPage_ReadsRemainingItemsOnly()
    {
        var stored = Contacts(25);
        Arrange(stored, skip: 20, take: 10);

        var result = await CreateHandler().Handle(new ListEmployeesQuery(3, 10), CancellationToken);

        result.Value.Items.Select(item => item.Id).Should().Equal(stored.Skip(20).Select(contact => contact.Id), "21 ~ 25번째 5건");
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 100, 0)]
    [InlineData(2, 1, 1)]
    [InlineData(100_000, 100, 9_999_900)]
    [InlineData(100_000, 1, 99_999)]
    public async Task Handle_Skip_IsPageMinusOneTimesPageSize(int page, int pageSize, int expectedSkip)
    {
        Arrange([], skip: expectedSkip, take: pageSize);

        await CreateHandler().Handle(new ListEmployeesQuery(page, pageSize), CancellationToken);

        await _repository.Received(1).ListOrderedByJoinedOnAsync(expectedSkip, pageSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RepositoryOrder_IsKeptAsIs()
    {
        // 정렬은 Repository(joined_on → id) 몫이다. Handler가 다시 정렬하면 등록 순(ID) 동률 처리가 달라질 수 있다.
        var later = new EmployeeContactResponse(Guid.NewGuid(), "나", "b@example.com", "010-0000-0002", new DateOnly(2021, 1, 1));
        var earlier = new EmployeeContactResponse(Guid.NewGuid(), "가", "a@example.com", "010-0000-0001", new DateOnly(2020, 1, 1));
        Arrange([later, earlier], skip: 0, take: 20);

        var result = await CreateHandler().Handle(new ListEmployeesQuery(1, 20), CancellationToken);

        result.Value.Items.Select(item => item.Id).Should().Equal(later.Id, earlier.Id);
    }

    private void Arrange(IReadOnlyList<EmployeeContactResponse> stored, int skip, int take)
    {
        _repository.ListOrderedByJoinedOnAsync(skip, take, Arg.Any<CancellationToken>()).Returns([.. stored.Skip(skip).Take(take)]);
        _repository.CountAsync(Arg.Any<CancellationToken>()).Returns(stored.Count);
    }

    private static List<EmployeeContactResponse> Contacts(int count) =>
        [.. Enumerable.Range(1, count).Select(number => new EmployeeContactResponse(
            Guid.NewGuid(), $"직원{number}", $"e{number}@example.com", "010-0000-0000", new DateOnly(2020, 1, 1).AddDays(number)))];

    private ListEmployeesQueryHandler CreateHandler() => new(_repository);
}
