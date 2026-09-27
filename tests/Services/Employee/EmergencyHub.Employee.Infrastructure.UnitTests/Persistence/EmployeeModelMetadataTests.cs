using System.Text;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S03-T02: employees 매핑을 설계 시점 모델(IDesignTimeModel)과 생성 SQL로 확인한다(DB 없음).
// 기준: S03 계획 리뷰 Q16 확정안(dba 기준 SQL), database.md Employee ERD · 인덱스 · 제약 표, dba 구현 사양 7.
[Trait("FR", "PRD-001/FR-08")]
[Trait("FR", "PRD-001/FR-06")]
public sealed class EmployeeModelMetadataTests : IDisposable
{
    private const int MaxIdentifierBytes = 63;

    private readonly EmployeeDbContext _context = EmployeeDbContexts.CreateWrite();

    private IModel Model => _context.GetService<IDesignTimeModel>().Model;

    private IEntityType EmployeeType => Model.FindEntityType(typeof(EmployeeAggregate))!;

    private ITable EmployeesTable => Model.GetRelationalModel().FindTable(EmployeeDbNames.EmployeesTable, null)!;

    public void Dispose() => _context.Dispose();

    // ---- 테이블 · 컬럼 ----

    [Fact]
    public void Tables_OfEmployeeModel_AreOnlyEmployeesWithoutSchema()
    {
        Model.GetRelationalModel().Tables.Select(table => (table.Name, table.Schema))
            .Should().Equal((EmployeeDbNames.EmployeesTable, (string?)null));
        EmployeeDbNames.EmployeesTable.Should().Be("employees");
    }

    [Fact]
    public void Columns_OfEmployees_MatchReferenceSqlTypesAndAreAllNotNull()
    {
        EmployeesTable.Columns.Select(column => (column.Name, column.StoreType, column.IsNullable)).Should().BeEquivalentTo(
        [
            ("id", "uuid", false),
            ("display_name", "character varying(100)", false),
            ("email", "character varying(254)", false),
            ("employee_status", "smallint", false),
            ("created_at", "timestamp with time zone", false),
            ("updated_at", "timestamp with time zone", false),
            ("xmin", "xid", false),
        ]);
    }

    [Fact]
    public void Columns_OfEmployees_HaveNoDefaultValues()
    {
        // uuid 기본값 없음(ADR-0013, Handler가 생성), DEFAULT 없음(dba 기준 SQL).
        EmployeesTable.Columns.Should().AllSatisfy(column =>
        {
            column.DefaultValueSql.Should().BeNull();
            column.DefaultValue.Should().BeNull();
            column.ComputedColumnSql.Should().BeNull();
        });
    }

    [Fact]
    public void DomainEvents_AreNotMapped()
    {
        EmployeeType.GetNavigations().Should().BeEmpty();
        EmployeeType.GetProperties().Select(property => property.Name).Should().BeEquivalentTo(
        [
            nameof(EmployeeAggregate.Id),
            nameof(EmployeeAggregate.DisplayName),
            nameof(EmployeeAggregate.Email),
            nameof(EmployeeAggregate.EmployeeStatus),
            ShadowPropertyNames.CreatedAt,
            ShadowPropertyNames.UpdatedAt,
            ShadowPropertyNames.Version,
        ]);
    }

    // ---- 키 · 인덱스 · 제약 ----

    [Fact]
    public void PrimaryKey_IsPkEmployeesOnId()
    {
        EmployeesTable.PrimaryKey!.Name.Should().Be("pk_employees");
        EmployeesTable.PrimaryKey.Columns.Select(column => column.Name).Should().Equal("id");
    }

    [Fact]
    public void Id_IsNeverGeneratedByDatabase()
    {
        EmployeeType.FindProperty(nameof(EmployeeAggregate.Id))!.ValueGenerated.Should().Be(ValueGenerated.Never);
    }

    [Fact]
    public void Indexes_OfEmployees_AreExactlyUniqueEmailIndexNamedByConstant()
    {
        var index = EmployeesTable.Indexes.Should().ContainSingle().Subject;

        index.Name.Should().Be(EmployeeDbNames.EmailUniqueIndex.Value);
        index.Name.Should().Be("ux_employees_email");
        index.IsUnique.Should().BeTrue();
        index.Columns.Select(column => column.Name).Should().Equal("email");
        EmployeeType.GetIndexes().Single().GetDatabaseName().Should().Be(EmployeeDbNames.EmailUniqueIndex.Value);
    }

    [Fact]
    public void CheckConstraints_OfEmployees_AreExactlyEmployeeStatusCodesWithoutReservedZero()
    {
        // EmployeeStatus에 Unknown = 0이 있어도 공통 규칙이 0을 빼 IN (1, 2)가 된다(S03-T01 reviewer 인계).
        var check = EmployeesTable.CheckConstraints.Should().ContainSingle().Subject;

        check.Name.Should().Be("ck_employees_employee_status");
        check.Sql.Should().Be("employee_status IN (1, 2)");
    }

    [Fact]
    public void Email_HasNoLowerCaseCheckConstraint()
    {
        // 정규화는 Domain 팩토리가 한다. CHECK (email = lower(email))는 두지 않는다(Q16 확정).
        EmployeesTable.CheckConstraints.Should().NotContain(check => check.Sql.Contains("lower", StringComparison.OrdinalIgnoreCase));
    }

    // ---- 감사 · 동시성 ----

    [Theory]
    [InlineData(ShadowPropertyNames.CreatedAt, "created_at")]
    [InlineData(ShadowPropertyNames.UpdatedAt, "updated_at")]
    public void AuditProperties_AreRequiredShadowTimestampTz(string propertyName, string columnName)
    {
        var property = EmployeeType.FindProperty(propertyName)!;

        property.IsShadowProperty().Should().BeTrue();
        property.ClrType.Should().Be<DateTimeOffset>();
        property.IsNullable.Should().BeFalse();
        property.GetColumnName().Should().Be(columnName);
        property.GetColumnType().Should().Be("timestamp with time zone");
    }

    [Fact]
    public void Version_IsShadowConcurrencyTokenMappedToXminXid()
    {
        var version = EmployeeType.FindProperty(ShadowPropertyNames.Version)!;

        version.IsShadowProperty().Should().BeTrue();
        version.IsConcurrencyToken.Should().BeTrue();
        version.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
        version.GetColumnName().Should().Be("xmin");
        version.GetColumnType().Should().Be("xid");
    }

    // ---- 생성 SQL ----

    [Fact]
    public void CreateScript_ContainsReferenceIndexAndNoXminOrDefault()
    {
        var script = _context.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE employees (");
        script.Should().Contain("CONSTRAINT pk_employees PRIMARY KEY (id)");
        script.Should().Contain("CONSTRAINT ck_employees_employee_status CHECK (employee_status IN (1, 2))");
        script.Should().Contain("CREATE UNIQUE INDEX ux_employees_email ON employees (email);");
        script.Should().NotContain("xmin");
        script.Should().NotContain("DEFAULT");
        script.Should().NotContain("ix_");
    }

    [Fact]
    public void Identifiers_OfEmployeesTable_AreAtMost63Bytes()
    {
        // 엣지: PostgreSQL은 63바이트를 넘는 이름을 경고 없이 자른다(잘린 이름은 23505 ConstraintName과 불일치).
        string[] identifiers =
        [
            EmployeesTable.Name,
            EmployeesTable.PrimaryKey!.Name,
            .. EmployeesTable.Columns.Select(column => column.Name),
            .. EmployeesTable.Indexes.Select(index => index.Name),
            .. EmployeesTable.CheckConstraints.Select(check => check.Name),
        ];

        identifiers.Should().AllSatisfy(identifier => Encoding.UTF8.GetByteCount(identifier).Should().BeLessThanOrEqualTo(MaxIdentifierBytes));
        identifiers.Max(identifier => Encoding.UTF8.GetByteCount(identifier)).Should().Be(28, "최장 식별자는 ck_employees_employee_status(dba 판정 e)");
    }
}
