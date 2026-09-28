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
// S05-T04: 기준은 database.md Employee "새 스키마 명세"(PRD-002 FR-01 · FR-02)와 dba 메타데이터 테스트 사양 15항목이다.
// 옛 이름(유니크 인덱스 · 표시 이름 컬럼)이 없다는 것은 리터럴 부정 단언 대신 인덱스 · 컬럼 · CREATE 줄의 정확한 집합 단언으로 확인한다.
[Trait("FR", "PRD-001/FR-08")]
[Trait("FR", "PRD-001/FR-06")]
[Trait("FR", "PRD-002/FR-01")]
[Trait("FR", "PRD-002/FR-02")]
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
            ("name", "character varying(100)", false),
            ("email", "character varying(254)", false),
            ("normalized_email", "character varying(254)", false),
            ("phone_number", "character varying(20)", false),
            ("joined_on", "date", false),
            ("employee_status", "smallint", false),
            ("created_at", "timestamp with time zone", false),
            ("updated_at", "timestamp with time zone", false),
            ("xmin", "xid", false),
        ]);
    }

    [Fact]
    public void Columns_InCreateScript_FollowPropertyDeclarationOrder()
    {
        // ITable.Columns는 선언 순서를 보장하지 않으므로 생성 SQL의 CREATE TABLE 블록을 줄 단위로 대조한다(HasColumnOrder 없음).
        var script = _context.Database.GenerateCreateScript();

        EmployeeCreateScript.TableLines(script).Should().Equal(EmployeeCreateScript.ExpectedTableLines);
    }

    [Fact]
    public void Columns_OfEmployees_HaveNoDefaultValues()
    {
        // uuid 기본값 없음(ADR-0013, Handler가 생성), employee_status Active=1도 Aggregate가 넣는다(DEFAULT 없음).
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
            nameof(EmployeeAggregate.Name),
            nameof(EmployeeAggregate.Email),
            nameof(EmployeeAggregate.NormalizedEmail),
            nameof(EmployeeAggregate.PhoneNumber),
            nameof(EmployeeAggregate.JoinedOn),
            nameof(EmployeeAggregate.EmployeeStatus),
            ShadowPropertyNames.CreatedAt,
            ShadowPropertyNames.UpdatedAt,
            ShadowPropertyNames.Version,
        ]);
    }

    // ---- Value Object 변환기 ----

    [Theory]
    [InlineData(nameof(EmployeeAggregate.Name), typeof(string), Name.MaxLength)]
    [InlineData(nameof(EmployeeAggregate.Email), typeof(string), Email.MaxLength)]
    [InlineData(nameof(EmployeeAggregate.PhoneNumber), typeof(string), PhoneNumber.MaxLength)]
    [InlineData(nameof(EmployeeAggregate.JoinedOn), typeof(DateOnly), null)]
    public void ValueObjectProperties_UseValueConverterToScalarProvider(string propertyName, Type providerType, int? maxLength)
    {
        // VO 4개는 EmployeeConfiguration 안의 HasConversion으로 스칼라 컬럼에 매핑한다(Owned · Complex 미사용, S05-T04).
        var property = EmployeeType.FindProperty(propertyName)!;

        property.GetValueConverter().Should().NotBeNull();
        property.GetValueConverter()!.ProviderClrType.Should().Be(providerType);
        property.GetMaxLength().Should().Be(maxLength);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void NormalizedEmail_IsPlainStringWithoutConverterAndEmailMaxLength()
    {
        var property = EmployeeType.FindProperty(nameof(EmployeeAggregate.NormalizedEmail))!;

        property.ClrType.Should().Be<string>();
        property.GetValueConverter().Should().BeNull();
        property.GetMaxLength().Should().Be(Email.MaxLength);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void ValueObjectConverters_RoundTripThroughCreate()
    {
        // DB → 모델 변환은 VO Create(...).Value를 거친다. JoinedOn은 DateOnly를 JoinedOn.Format · InvariantCulture 문자열로 바꿔 넘긴다.
        var joinedOn = EmployeeType.FindProperty(nameof(EmployeeAggregate.JoinedOn))!.GetValueConverter()!;
        var name = EmployeeType.FindProperty(nameof(EmployeeAggregate.Name))!.GetValueConverter()!;
        var email = EmployeeType.FindProperty(nameof(EmployeeAggregate.Email))!.GetValueConverter()!;

        joinedOn.ConvertFromProvider(new DateOnly(1900, 1, 1)).Should().Be(JoinedOn.Create("1900-01-01").Value);
        joinedOn.ConvertToProvider(JoinedOn.Create("2020-03-02").Value).Should().Be(new DateOnly(2020, 3, 2));
        name.ConvertFromProvider("홍길동").Should().Be(Name.Create("홍길동").Value);
        email.ConvertToProvider(Email.Create("Hong@Example.com").Value).Should().Be("Hong@Example.com", "email 컬럼은 입력 표기를 저장한다");
    }

    [Theory]
    [InlineData(nameof(EmployeeAggregate.Name), "홍\u0007길동")]
    [InlineData(nameof(EmployeeAggregate.Email), "not-an-email")]
    [InlineData(nameof(EmployeeAggregate.PhoneNumber), "+82-10-1234-5678")]
    public void ValueObjectConverters_ProviderValueBreakingRule_Throw(string propertyName, string providerValue)
    {
        // 실패: DB에는 이 규칙의 ck가 없다. DB 값이 VO 규칙을 어기면 구체화 때 예외다(값은 메시지에 담기지 않음).
        var converter = EmployeeType.FindProperty(propertyName)!.GetValueConverter()!;

        var act = () => converter.ConvertFromProvider(providerValue);

        act.Should().Throw<InvalidOperationException>().Which.Message.Should().NotContain(providerValue);
    }

    [Fact]
    public void JoinedOnConverter_ProviderDateBeforeMinValue_Throws()
    {
        // 엣지: joined_on 하한(1900-01-01)은 DB ck가 없는 Domain 규칙이라 1899-12-31은 구체화 때 막힌다.
        var converter = EmployeeType.FindProperty(nameof(EmployeeAggregate.JoinedOn))!.GetValueConverter()!;

        var act = () => converter.ConvertFromProvider(new DateOnly(1899, 12, 31));

        act.Should().Throw<InvalidOperationException>();
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
    public void Indexes_OfEmployees_AreExactlyNormalizedEmailUniqueAndTwoConventionNamedIndexes()
    {
        EmployeesTable.Indexes.Select(index => (index.Name, index.IsUnique, Columns: string.Join(",", index.Columns.Select(column => column.Name))))
            .Should().BeEquivalentTo(
            [
                ("ux_employees_normalized_email", true, "normalized_email"),
                ("ix_employees_joined_on_id", false, "joined_on,id"),
                ("ix_employees_name_joined_on_id", false, "name,joined_on,id"),
            ]);
        EmployeeType.GetIndexes().Select(index => index.GetDatabaseName())
            .Should().BeEquivalentTo("ux_employees_normalized_email", "ix_employees_joined_on_id", "ix_employees_name_joined_on_id");
    }

    [Fact]
    public void UniqueIndex_IsNamedByConstantOnNormalizedEmailColumnOnly()
    {
        var index = EmployeesTable.Indexes.Should().ContainSingle(index => index.IsUnique).Subject;

        index.Name.Should().Be(EmployeeDbNames.NormalizedEmailUniqueIndex.Value);
        EmployeeDbNames.NormalizedEmailUniqueIndex.Value.Should().Be("ux_employees_normalized_email");
        index.Columns.Select(column => column.Name).Should().Equal("normalized_email");
    }

    [Theory]
    [InlineData("ux_employees_normalized_email", true)]
    [InlineData("ix_employees_joined_on_id", false)]
    [InlineData("ix_employees_name_joined_on_id", false)]
    public void IndexNames_OnlyUniqueIndexIsExplicitAndIxIndexesComeFromNamingConvention(string databaseName, bool isExplicit)
    {
        // ix_ 이름은 명명 규칙(EFCore.NamingConventions)이 만들고 HasDatabaseName을 쓰지 않는다. ux_만 이름 상수(HasUniqueIndex)다.
        var index = EmployeeType.GetIndexes().Single(index => index.GetDatabaseName() == databaseName);

        var source = ((IConventionIndex)index).GetDatabaseNameConfigurationSource();

        (source == ConfigurationSource.Explicit).Should().Be(isExplicit);
    }

    [Fact]
    public void Indexes_DoNotIncludeEmailInputColumn()
    {
        // 엣지: 입력 표기 email에는 인덱스를 두지 않는다(유일성은 normalized_email, ADR-0027).
        EmployeesTable.Indexes.Should().NotContain(index => index.Columns.Any(column => column.Name == "email"));
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
        // 정규화는 Domain(Email VO)이 한다. CHECK (normalized_email = lower(email))는 두지 않는다(ADR-0027, U+0130 실측).
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
    public void CreateScript_ContainsReferenceIndexesAndNoXminOrDefault()
    {
        var script = _context.Database.GenerateCreateScript();

        script.Should().Contain(EmployeeCreateScript.CreateTableHeader);
        script.Should().Contain("CONSTRAINT pk_employees PRIMARY KEY (id)");
        script.Should().Contain("CONSTRAINT ck_employees_employee_status CHECK (employee_status IN (1, 2))");
        script.Should().Contain(EmployeeCreateScript.UniqueIndexSql);
        script.Should().Contain(EmployeeCreateScript.JoinedOnIdIndexSql);
        script.Should().Contain(EmployeeCreateScript.NameJoinedOnIdIndexSql);
        EmployeeCreateScript.Count(script, "CREATE INDEX ").Should().Be(2, "ix_ 인덱스는 2개뿐이다(email 인덱스 · 옛 유니크 인덱스 없음)");
        EmployeeCreateScript.Count(script, "CREATE UNIQUE INDEX ").Should().Be(1);
        script.Should().NotContain("xmin");
        script.Should().NotContain("DEFAULT");
        script.Should().NotContain("CONCURRENTLY");
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
        identifiers.Max(identifier => Encoding.UTF8.GetByteCount(identifier))
            .Should().Be(30, "최장 식별자는 ix_employees_name_joined_on_id(database.md 새 스키마 명세)");
    }
}
