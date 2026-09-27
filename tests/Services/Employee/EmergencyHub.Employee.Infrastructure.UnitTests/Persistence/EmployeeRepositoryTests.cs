using System.Data;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence.Repositories;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S03-T02: Write Repository는 쿼리만 담는다(coding-conventions "Repository 규칙"). DB 없이 명령을 가로채 SQL · 매개변수를 확인한다.
// 이메일은 호출자가 정규화한 값을 그대로 비교한다(ToLower · Trim 없음, dba 구현 사양 4).
[Trait("FR", "PRD-001/FR-08")]
public sealed class EmployeeRepositoryTests
{
    [Fact]
    public async Task ExistsByEmailAsync_MatchingRow_ReturnsTrue()
    {
        var database = new FakeQueryDatabase(ExistsResult(true));
        await using var context = EmployeeDbContexts.CreateWrite(database);

        var exists = await new EmployeeRepository(context).ExistsByEmailAsync("hong@example.com", TestContext.Current.CancellationToken);

        exists.Should().BeTrue();
        database.CommandTexts.Should().ContainSingle().Which.Should().Contain("EXISTS").And.Contain("FROM employees");
    }

    [Fact]
    public async Task ExistsByEmailAsync_NoRow_ReturnsFalse()
    {
        var database = new FakeQueryDatabase(ExistsResult(false));
        await using var context = EmployeeDbContexts.CreateWrite(database);

        var exists = await new EmployeeRepository(context).ExistsByEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByEmailAsync_MixedCaseValue_IsComparedAsGivenWithoutCaseFolding()
    {
        // 엣지: 정규화는 Domain(EmployeeEmail.Normalize) 몫이다. Repository가 lower() · trim()을 넣으면 인덱스를 못 쓴다.
        var database = new FakeQueryDatabase(ExistsResult(false));
        await using var context = EmployeeDbContexts.CreateWrite(database);

        await new EmployeeRepository(context).ExistsByEmailAsync(" Hong@Example.com ", TestContext.Current.CancellationToken);

        var sql = database.CommandTexts.Should().ContainSingle().Subject;
        sql.Should().Contain("e.email = @");
        sql.Should().NotContainAny("lower(", "upper(", "trim(");
        database.ParameterValues.Should().Equal(" Hong@Example.com ");
    }

    [Fact]
    public async Task Add_NewEmployee_IsTrackedAsAddedWithoutSaving()
    {
        var database = new FakeQueryDatabase(ExistsResult(false));
        await using var context = EmployeeDbContexts.CreateWrite(database);
        var employee = EmployeeDbContexts.NewEmployee();

        new EmployeeRepository(context).Add(employee);

        context.Entry(employee).State.Should().Be(EntityState.Added);
        database.CommandTexts.Should().BeEmpty("저장은 UnitOfWork가 한다");
    }

    [Fact]
    public async Task WriteContext_MaterializesEmployeeThroughPrivateConstructor()
    {
        // S03-T01 tester 인계: EF용 private 매개변수 없는 생성자로 구체화된다. 결과 열 순서는 EF가 만드는 SELECT 순서다.
        var id = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero);
        var result = new DataTable();
        result.Columns.Add("id", typeof(Guid));
        result.Columns.Add("created_at", typeof(DateTimeOffset));
        result.Columns.Add("display_name", typeof(string));
        result.Columns.Add("email", typeof(string));
        result.Columns.Add("employee_status", typeof(short));
        result.Columns.Add("updated_at", typeof(DateTimeOffset));
        result.Columns.Add("xmin", typeof(uint));
        result.Rows.Add(id, at, "홍길동", "hong@example.com", (short)EmployeeStatus.Inactive, at, 7u);
        await using var context = EmployeeDbContexts.CreateWrite(new FakeQueryDatabase(result));

        var employee = await context.Set<EmployeeAggregate>().SingleAsync(TestContext.Current.CancellationToken);

        employee.Id.Should().Be(new EmployeeId(id));
        employee.DisplayName.Should().Be("홍길동");
        employee.Email.Should().Be("hong@example.com");
        employee.EmployeeStatus.Should().Be(EmployeeStatus.Inactive);
        employee.DomainEvents.Should().BeEmpty();
        context.Entry(employee).Property<uint>(ShadowPropertyNames.Version).CurrentValue.Should().Be(7u);
    }

    private static DataTable ExistsResult(bool exists)
    {
        var table = new DataTable();
        table.Columns.Add("exists", typeof(bool));
        table.Rows.Add(exists);
        return table;
    }
}
