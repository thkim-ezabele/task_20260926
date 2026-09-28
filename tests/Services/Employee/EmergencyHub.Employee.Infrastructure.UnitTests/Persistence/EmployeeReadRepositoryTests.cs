using System.Data;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence.ReadRepositories;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S05-T06(PRD-002 FR-07 · FR-08): Read Repository는 읽기 DbContext에서 응답 record로 바로 프로젝션한다. DB 없이 명령을 가로채 SQL 모양을 확인한다.
// 기대 SQL의 원본은 dba 쿼리 명세(목록 ORDER BY joined_on, id + LIMIT / OFFSET, 개수는 별도 count(*), 이름은 name = @p ORDER BY joined_on, id LIMIT 1).
// 실제 DB 결과 · 인덱스 사용은 통합 테스트(EmployeeReadRepositoryDatabaseTests · EmployeeQueryPlanTests)가 확인한다.
[Trait("FR", "PRD-002/FR-07")]
public sealed class EmployeeReadRepositoryTests
{
    private static readonly Guid Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000001");

    [Fact]
    public async Task ListOrderedByJoinedOnAsync_Page_OrdersByJoinedOnThenIdWithLimitAndOffsetParameters()
    {
        var database = new FakeQueryDatabase(ContactRows(("홍길동", "Hong@Example.com")));
        await using var context = EmployeeDbContexts.CreateRead(database);

        var items = await new EmployeeReadRepository(context).ListOrderedByJoinedOnAsync(10, 20, TestContext.Current.CancellationToken);

        items.Should().Equal(new EmployeeContactResponse(Id, "홍길동", "Hong@Example.com", "010-1234-5678", new DateOnly(2020, 3, 2)));
        var sql = database.CommandTexts.Should().ContainSingle().Subject;
        sql.Should().Contain("SELECT e.id, e.name, e.email, e.phone_number, e.joined_on").And.Contain("FROM employees AS e");
        sql.Should().Contain("ORDER BY e.joined_on, e.id").And.Contain("LIMIT @").And.Contain("OFFSET @");
        database.ParameterValues.Should().BeEquivalentTo([20, 10], "take · skip은 값이 아니라 매개변수로 간다");
    }

    [Fact]
    public async Task CountAsync_All_SendsSeparateCountWithoutWindowFunction()
    {
        // 목록과 개수는 메서드를 나누고 COUNT(*) OVER()를 쓰지 않는다(PRD-002 FR-07).
        var database = new FakeQueryDatabase(CountRow(25));
        await using var context = EmployeeDbContexts.CreateRead(database);

        var count = await new EmployeeReadRepository(context).CountAsync(TestContext.Current.CancellationToken);

        count.Should().Be(25);
        var sql = database.CommandTexts.Should().ContainSingle().Subject;
        sql.Should().Contain("count(*)").And.Contain("FROM employees AS e");
        sql.Should().NotContainAny("OVER", "ORDER BY", "LIMIT");
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_Name_ComparesNameColumnAndTakesFirstByJoinedOnThenId()
    {
        // 동명이인이면 입사일이 빠른 1명, 같으면 등록 순(id)이다(PRD-002 FR-08). 이름은 Name VO끼리 비교해 name = @p로 번역된다.
        var database = new FakeQueryDatabase(ContactRows(("홍길동", "Hong@Example.com")));
        await using var context = EmployeeDbContexts.CreateRead(database);

        var found = await new EmployeeReadRepository(context).FindFirstByNameAsync(Name.Create("홍길동").Value, TestContext.Current.CancellationToken);

        found.Should().Be(new EmployeeContactResponse(Id, "홍길동", "Hong@Example.com", "010-1234-5678", new DateOnly(2020, 3, 2)));
        var sql = database.CommandTexts.Should().ContainSingle().Subject;
        sql.Should().Contain("WHERE e.name = @").And.Contain("ORDER BY e.joined_on, e.id").And.Contain("LIMIT 1");
        sql.Should().NotContainAny("lower(", "upper(", "trim(");
        database.ParameterValues.Should().Equal("홍길동");
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_NoRow_ReturnsNull()
    {
        var database = new FakeQueryDatabase(ContactRows());
        await using var context = EmployeeDbContexts.CreateRead(database);

        var found = await new EmployeeReadRepository(context).FindFirstByNameAsync(Name.Create("없는 이름").Value, TestContext.Current.CancellationToken);

        found.Should().BeNull();
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_NfdInput_SendsNfcValue()
    {
        // 엣지: 정규화는 Name.Create(Trim + NFC) 몫이라 Repository는 VO 값을 그대로 보낸다. NFD로 들어온 이름도 NFC 값으로 비교된다.
        var database = new FakeQueryDatabase(ContactRows());
        await using var context = EmployeeDbContexts.CreateRead(database);
        var nfd = "홍길동".Normalize(System.Text.NormalizationForm.FormD);

        await new EmployeeReadRepository(context).FindFirstByNameAsync(Name.Create($" {nfd} ").Value, TestContext.Current.CancellationToken);

        database.ParameterValues.Should().Equal("홍길동");
    }

    // 결과 열 순서는 프로젝션 순서(id, name, email, phone_number, joined_on)다.
    private static DataTable ContactRows(params (string Name, string Email)[] rows)
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("email", typeof(string));
        table.Columns.Add("phone_number", typeof(string));
        table.Columns.Add("joined_on", typeof(DateOnly));
        foreach (var (name, email) in rows)
        {
            table.Rows.Add(Id, name, email, "010-1234-5678", new DateOnly(2020, 3, 2));
        }

        return table;
    }

    private static DataTable CountRow(int count)
    {
        var table = new DataTable();
        table.Columns.Add("count", typeof(int));
        table.Rows.Add(count);
        return table;
    }
}
