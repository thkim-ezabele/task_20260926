using System.Data;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence.ReadRepositories;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S03-T02: Read Repository는 읽기 DbContext에서 EmployeeResponse로 바로 프로젝션한다(ADR-0007).
// 감사 시각은 shadow property(ShadowPropertyNames), xmin은 노출하지 않는다. DB 없이 명령을 가로채 확인한다.
[Trait("FR", "PRD-001/FR-08")]
public sealed class EmployeeReadRepositoryTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 27, 1, 2, 3, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 9, 27, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    public async Task GetByIdAsync_ExistingRow_ProjectsAllResponseFields()
    {
        var id = Guid.NewGuid();
        var result = ProjectionResult();
        result.Rows.Add(id, "홍길동", "hong@example.com", (short)EmployeeStatus.Active, CreatedAt, UpdatedAt);
        await using var context = EmployeeDbContexts.CreateRead(new FakeQueryDatabase(result));

        var response = await new EmployeeReadRepository(context).GetByIdAsync(id, TestContext.Current.CancellationToken);

        response.Should().Be(new EmployeeResponse(id, "홍길동", "hong@example.com", EmployeeStatus.Active, CreatedAt, UpdatedAt));
    }

    [Fact]
    public async Task GetByIdAsync_NoRow_ReturnsNull()
    {
        await using var context = EmployeeDbContexts.CreateRead(new FakeQueryDatabase(ProjectionResult()));

        var response = await new EmployeeReadRepository(context).GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        response.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Sql_FiltersByIdParameterSelectsAuditColumnsAndNotXmin()
    {
        var id = Guid.NewGuid();
        var database = new FakeQueryDatabase(ProjectionResult());
        await using var context = EmployeeDbContexts.CreateRead(database);

        await new EmployeeReadRepository(context).GetByIdAsync(id, TestContext.Current.CancellationToken);

        var sql = database.CommandTexts.Should().ContainSingle().Subject;
        sql.Should().Contain("FROM employees").And.Contain("e.id = @").And.Contain("LIMIT 1");
        sql.Should().Contain("e.created_at").And.Contain("e.updated_at");
        sql.Should().NotContain("xmin");
        database.ParameterValues.Should().Equal(id);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyGuid_QueriesAndReturnsNullWithoutThrowing()
    {
        // 엣지: 빈 Guid는 Domain이 만들지 않는 ID지만 조회는 예외 없이 "없음"이다(Handler가 22001로 바꿈).
        var database = new FakeQueryDatabase(ProjectionResult());
        await using var context = EmployeeDbContexts.CreateRead(database);

        var response = await new EmployeeReadRepository(context).GetByIdAsync(Guid.Empty, TestContext.Current.CancellationToken);

        response.Should().BeNull();
        database.ParameterValues.Should().Equal(Guid.Empty);
    }

    private static DataTable ProjectionResult()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("display_name", typeof(string));
        table.Columns.Add("email", typeof(string));
        table.Columns.Add("employee_status", typeof(short));
        table.Columns.Add("created_at", typeof(DateTimeOffset));
        table.Columns.Add("updated_at", typeof(DateTimeOffset));
        return table;
    }
}
