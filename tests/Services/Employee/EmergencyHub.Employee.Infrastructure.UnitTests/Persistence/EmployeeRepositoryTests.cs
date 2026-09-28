using System.Data;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence.Repositories;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S03-T02: Write Repository는 쿼리만 담는다(coding-conventions "Repository 규칙"). DB 없이 명령을 가로채 SQL · 매개변수를 확인한다.
// 정규화 이메일은 호출자(Email VO)가 만든 값을 normalized_email과 그대로 비교한다(ToLower · Trim 없음, ADR-0027).
[Trait("FR", "PRD-001/FR-08")]
public sealed class EmployeeRepositoryTests
{
    [Fact]
    public async Task ExistsByNormalizedEmailAsync_MatchingRow_ReturnsTrue()
    {
        var database = new FakeQueryDatabase(ExistsResult(true));
        await using var context = EmployeeDbContexts.CreateWrite(database);

        var exists = await new EmployeeRepository(context).ExistsByNormalizedEmailAsync("hong@example.com", TestContext.Current.CancellationToken);

        exists.Should().BeTrue();
        database.CommandTexts.Should().ContainSingle().Which.Should().Contain("EXISTS").And.Contain("FROM employees");
    }

    [Fact]
    public async Task ExistsByNormalizedEmailAsync_NoRow_ReturnsFalse()
    {
        var database = new FakeQueryDatabase(ExistsResult(false));
        await using var context = EmployeeDbContexts.CreateWrite(database);

        var exists = await new EmployeeRepository(context).ExistsByNormalizedEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNormalizedEmailAsync_MixedCaseValue_IsComparedAsGivenWithoutCaseFolding()
    {
        // 엣지: 정규화는 Domain(Email.NormalizedEmail) 몫이다. Repository가 lower() · trim()을 넣으면 인덱스를 못 쓴다.
        var database = new FakeQueryDatabase(ExistsResult(false));
        await using var context = EmployeeDbContexts.CreateWrite(database);

        await new EmployeeRepository(context).ExistsByNormalizedEmailAsync(" Hong@Example.com ", TestContext.Current.CancellationToken);

        var sql = database.CommandTexts.Should().ContainSingle().Subject;
        sql.Should().Contain("e.normalized_email = @");
        sql.Should().NotContain("e.email = @", "입력 표기 컬럼(email)은 비교하지 않는다");
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
    public async Task WriteContext_MaterializesEmployeeThroughPrivateConstructorAndValueObjectConverters()
    {
        // S03-T01 tester 인계: EF용 private 매개변수 없는 생성자로 구체화된다. VO 4개는 값 변환기(Create(...).Value)를 거친다(S05-T04).
        var id = Guid.NewGuid();
        var result = EmployeeRow(id, name: "홍길동", joinedOn: new DateOnly(1900, 1, 1));
        await using var context = EmployeeDbContexts.CreateWrite(new FakeQueryDatabase(result));

        var employee = await context.Set<EmployeeAggregate>().SingleAsync(TestContext.Current.CancellationToken);

        employee.Id.Should().Be(new EmployeeId(id));
        employee.Name.Should().Be(Name.Create("홍길동").Value);
        employee.Email.Should().Be(Email.Create("Hong@Example.com").Value);
        employee.Email.Value.Should().Be("Hong@Example.com");
        employee.NormalizedEmail.Should().Be("hong@example.com");
        employee.PhoneNumber.Should().Be(PhoneNumber.Create("010-1234-5678").Value);
        employee.JoinedOn.Should().Be(JoinedOn.Create("1900-01-01").Value);
        employee.EmployeeStatus.Should().Be(EmployeeStatus.Inactive);
        employee.DomainEvents.Should().BeEmpty();
        context.Entry(employee).Property<uint>(ShadowPropertyNames.Version).CurrentValue.Should().Be(7u);
    }

    [Theory]
    [InlineData("   ", "2020-03-02")]
    [InlineData("홍길동", "1899-12-31")]
    public async Task WriteContext_StoredValueBreakingValueObjectRule_ThrowsOnMaterialization(string name, string joinedOn)
    {
        // 실패: DB에는 이름 · 입사일 규칙의 ck가 없다. 규칙을 어긴 값은 구체화 때 Create(...).Value에서 예외가 난다(database.md 새 스키마 명세).
        var result = EmployeeRow(Guid.NewGuid(), name, DateOnly.ParseExact(joinedOn, JoinedOn.Format, System.Globalization.CultureInfo.InvariantCulture));
        await using var context = EmployeeDbContexts.CreateWrite(new FakeQueryDatabase(result));

        var act = () => context.Set<EmployeeAggregate>().SingleAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static DataTable EmployeeRow(Guid id, string name, DateOnly joinedOn)
    {
        // 결과 열 순서는 EF가 만드는 SELECT 순서(키 → 속성 이름 순, shadow 포함)다.
        var at = new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero);
        var result = new DataTable();
        result.Columns.Add("id", typeof(Guid));
        result.Columns.Add("created_at", typeof(DateTimeOffset));
        result.Columns.Add("email", typeof(string));
        result.Columns.Add("employee_status", typeof(short));
        result.Columns.Add("joined_on", typeof(DateOnly));
        result.Columns.Add("name", typeof(string));
        result.Columns.Add("normalized_email", typeof(string));
        result.Columns.Add("phone_number", typeof(string));
        result.Columns.Add("updated_at", typeof(DateTimeOffset));
        result.Columns.Add("xmin", typeof(uint));
        result.Rows.Add(id, at, "Hong@Example.com", (short)EmployeeStatus.Inactive, joinedOn, name, "hong@example.com", "010-1234-5678", at, 7u);
        return result;
    }

    private static DataTable ExistsResult(bool exists)
    {
        var table = new DataTable();
        table.Columns.Add("exists", typeof(bool));
        table.Rows.Add(exists);
        return table;
    }
}
