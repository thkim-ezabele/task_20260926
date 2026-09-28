using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Schema;

// BL-082 스키마 · 연결 실측(testing-strategy.md "DB 검증 쿼리" Q1 · Q2 · Q3 · Q4 · Q6 · Q7)과 FR-08 인수 조건
// "체크 제약 위반 거부", "읽기 연결로 쓰기 시도 시 DB 거부", "UUID v7의 DB 정렬 순서"를 마이그레이션을 적용한 실제 스키마에서 확인한다.
// 이름 판정은 EF 모델(운영 등록) · 이름 상수와 서버 카탈로그를 대조하고, 체크 식은 서버가 정규화하므로 텍스트로 비교하지 않는다(값은 삽입 결과로 판정).
// S05-T04: 기대값을 새 스키마 명세(database.md, PRD-002 FR-01 · FR-02)로 바꿨다. 적용은 S05-T05(InitialCreate 재생성) 뒤라 그 전에는 실패 허용 목록이다.
// 원시 INSERT는 DB가 강제하지 않는 규칙(normalized_email = ToLowerInvariant(email), 이름 · 전화 · 입사일 형식)에 맞는 값만 넣는다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-08")]
public sealed class EmployeeSchemaTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string CheckConstraintName = "ck_employees_employee_status";
    private const string InsertSql =
        "INSERT INTO employees (id, name, email, normalized_email, phone_number, joined_on, employee_status, created_at, updated_at) "
        + "VALUES ($1, 'Schema Test', $2, $3, '010-1234-5678', DATE '2020-03-02', $4, now(), now())";

    private const int SameMillisecondIds = 1000;
    private const int MaxSameMillisecondAttempts = 50;

    // ---- 성공 ----

    [Fact]
    public async Task Columns_AfterMigration_MatchSampleTableTypesWithoutDefaults()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
            FROM information_schema.columns WHERE table_schema = 'public' AND table_name = $1 ORDER BY ordinal_position
            """,
            connection);
        command.Parameters.Add(new NpgsqlParameter { Value = EmployeeDbNames.EmployeesTable });
        var columns = new List<(string Name, string Type, int? Length, string Nullable, string? Default)>();

        await using (var reader = await command.ExecuteReaderAsync(CancellationToken))
        {
            while (await reader.ReadAsync(CancellationToken))
            {
                columns.Add((reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4)));
            }
        }

        columns.Should().Equal(
            ("id", "uuid", null, "NO", null),
            ("name", "character varying", Name.MaxLength, "NO", null),
            ("email", "character varying", Email.MaxLength, "NO", null),
            ("normalized_email", "character varying", Email.MaxLength, "NO", null),
            ("phone_number", "character varying", PhoneNumber.MaxLength, "NO", null),
            ("joined_on", "date", null, "NO", null),
            ("employee_status", "smallint", null, "NO", null),
            ("created_at", "timestamp with time zone", null, "NO", null),
            ("updated_at", "timestamp with time zone", null, "NO", null));
    }

    [Fact]
    public async Task ConstraintAndIndexNames_AfterMigration_EqualEfModelNamesAndConstants()
    {
        // 23505 · 23514의 ConstraintName 판정(UnitOfWork 매핑 · 로그)은 서버 이름 = 모델 이름 = 상수에 기대므로 셋을 대조한다.
        await using var services = Database.CreateServices();
        await using var scope = services.CreateAsyncScope();
        // 체크 제약은 실행 모델에 없어 설계 시점 모델(마이그레이션이 쓰는 모델)에서 읽는다.
        var entityType = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Domain.Employees.Employee))!;
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var constraints = await ReadStringsAsync(
            connection, "SELECT conname || ':' || contype::text FROM pg_constraint WHERE conrelid = 'public.employees'::regclass ORDER BY conname");
        var indexes = await ReadStringsAsync(connection, "SELECT indexname FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'employees' ORDER BY indexname");

        entityType.GetTableName().Should().Be(EmployeeDbNames.EmployeesTable);
        constraints.Should().Equal($"{CheckConstraintName}:c", "pk_employees:p");
        entityType.GetCheckConstraints().Select(check => check.Name).Should().Equal(CheckConstraintName);
        entityType.FindPrimaryKey()!.GetName().Should().Be("pk_employees");
        indexes.Should().Equal("ix_employees_joined_on_id", "ix_employees_name_joined_on_id", "pk_employees", EmployeeDbNames.NormalizedEmailUniqueIndex.Value);
        entityType.GetIndexes().Select(index => index.GetDatabaseName()).Should().BeEquivalentTo(
            "ix_employees_joined_on_id", "ix_employees_name_joined_on_id", EmployeeDbNames.NormalizedEmailUniqueIndex.Value);
    }

    [Theory]
    [InlineData(EmployeeStatus.Active)]
    [InlineData(EmployeeStatus.Inactive)]
    public async Task RawInsert_DefinedStatus_IsAccepted(EmployeeStatus status)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var rows = await connection.ExecuteSqlAsync(InsertSql, CancellationToken, InsertParameters((int)status));

        rows.Should().Be(1);
    }

    [Fact]
    [Trait("FR", "PRD-001/FR-06")]
    public async Task OrderById_IdsGeneratedWithinSameMillisecondInsertedShuffled_ReturnsGenerationOrder()
    {
        // S3 · Q7: 운영 IIdGenerator(UUIDNext PostgreSql 형식)는 같은 밀리초 안에서도 단조 증가하고, PostgreSQL uuid 비교(바이트 순)와 순서가 같다.
        // S05-T06: 같은 밀리초 안 1,000건(일괄 등록 상한)으로 확인한다. 1,000건이 한 밀리초에 모두 들어간 묶음이 나올 때까지 다시 만든다
        // (로컬 실측: 1,000건 30회 중 11회가 한 밀리초, 나머지는 두 밀리초에 걸침).
        await using var services = Database.CreateServices();
        await using var scope = services.CreateAsyncScope();
        var generator = scope.ServiceProvider.GetRequiredService<IIdGenerator>();
        var generated = Enumerable.Range(0, MaxSameMillisecondAttempts)
            .Select(_ => Enumerable.Range(0, SameMillisecondIds).Select(_ => generator.NewId()).ToList())
            .FirstOrDefault(batch => batch.Select(UnixMilliseconds).Distinct().Count() == 1);
        generated.Should().NotBeNull($"{MaxSameMillisecondAttempts}번 안에 1,000건이 한 밀리초에 들어간 묶음이 있어야 이 검증이 의미 있다");
        var repository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        foreach (var id in generated!.OrderBy(_ => Random.Shared.Next()))
        {
            repository.Add(new EmployeeBuilder().WithId(new EmployeeId(id)).Build());
        }

        (await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken)).IsSuccess.Should().BeTrue();

        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        var ordered = new List<Guid>();
        await using var command = new NpgsqlCommand("SELECT id FROM employees ORDER BY id", connection);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
        {
            ordered.Add(reader.GetGuid(0));
        }

        ordered.Should().Equal(generated);
    }

    // ---- 실패 ----

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(9)]
    [InlineData(-1)]
    public async Task RawInsert_UndefinedOrReservedStatus_IsRejectedByCheckConstraint23514(int status)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var act = () => connection.ExecuteSqlAsync(InsertSql, CancellationToken, InsertParameters(status));

        var exception = (await act.Should().ThrowAsync<PostgresException>()).Which;
        (exception.SqlState, exception.ConstraintName, exception.TableName)
            .Should().Be((PostgresErrorCodes.CheckViolation, CheckConstraintName, EmployeeDbNames.EmployeesTable));
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)9)]
    [InlineData((short)-1)]
    [InlineData(short.MaxValue)]
    public async Task CommitAsync_TrackedEntryWithUndefinedStatus_RethrowsDbUpdateExceptionWithCheckConstraintAndRedactedDetail(short status)
    {
        // S4 · Q4: 23514는 UnitOfWork가 변환하지 않는다(전역 예외 처리기 9001 경로). 서버 DETAIL(행 값)은 Include Error Detail이 없어 가려진다.
        await using var services = Database.CreateServices();
        await using var scope = services.CreateAsyncScope();
        var employee = new EmployeeBuilder().Build();
        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Entry(employee).Property(e => e.EmployeeStatus).CurrentValue = (EmployeeStatus)status;

        var act = () => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken);

        var inner = (await act.Should().ThrowAsync<DbUpdateException>()).WithInnerException<PostgresException>().Which;
        (inner.SqlState, inner.ConstraintName).Should().Be((PostgresErrorCodes.CheckViolation, CheckConstraintName));
        inner.Detail.Should().StartWith("Detail redacted").And.NotContain(employee.Email.Value).And.NotContain(employee.NormalizedEmail);
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>("SELECT count(*) FROM employees", CancellationToken)).Should().Be(0);
    }

    [Theory]
    [InlineData("UPDATE employees SET name = 'changed'")]
    [InlineData("DELETE FROM employees")]
    [InlineData("TRUNCATE employees")]
    public async Task ReadConnection_WriteStatement_IsRejectedWith25006(string sql)
    {
        // S2 · Q6(INSERT는 EmployeeDatabaseFixtureTests): 읽기 연결의 모든 쓰기 문장은 서버가 거부한다(행이 없어도 문장 단계에서 거부).
        await using var connection = await Database.OpenReadConnectionAsync(CancellationToken);

        var act = () => connection.ExecuteSqlAsync(sql, CancellationToken);

        var exception = (await act.Should().ThrowAsync<PostgresException>()).Which;
        (exception.SqlState, exception.ConstraintName).Should().Be((PostgresErrorCodes.ReadOnlySqlTransaction, null));
    }

    [Fact]
    public async Task ReadConnection_InsertInsideExplicitReadCommittedTransaction_IsStillRejectedWith25006()
    {
        await using var connection = await Database.OpenReadConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<string>("SHOW default_transaction_read_only", CancellationToken)).Should().Be("on");
        await connection.ExecuteSqlAsync("BEGIN ISOLATION LEVEL READ COMMITTED", CancellationToken);

        var act = () => connection.ExecuteSqlAsync(InsertSql, CancellationToken, InsertParameters((int)EmployeeStatus.Active));

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.ReadOnlySqlTransaction);
    }

    [Fact]
    public async Task ReadDbContext_RawInsertThroughProductionRegistration_IsRejectedWith25006()
    {
        // 운영 등록(AddEmployeeInfrastructure)의 읽기 DbContext가 read-only 연결(ConnectionStrings:Read)을 쓴다. SaveChanges 차단(단위 테스트)과 별개로 DB가 막는다.
        await using var services = Database.CreateServices();
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeReadDbContext>();
        var (id, email, status) = (Guid.NewGuid(), NewEmail(), (int)EmployeeStatus.Active);
        FormattableString insert =
            $"INSERT INTO employees (id, name, email, normalized_email, phone_number, joined_on, employee_status, created_at, updated_at) VALUES ({id}, {"Schema Test"}, {email}, {email.ToLowerInvariant()}, {"010-1234-5678"}, {new DateOnly(2020, 3, 2)}, {status}, now(), now())";

        var act = () => db.Database.ExecuteSqlAsync(insert, CancellationToken);

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.ReadOnlySqlTransaction);
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>("SELECT count(*) FROM employees", CancellationToken)).Should().Be(0);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task RawInsert_StatusOutsideSmallintRange_IsRejectedByColumnType22003()
    {
        // employee_status는 smallint(ADR-0008): 32768은 체크 제약 전에 형식 범위에서 거부된다.
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);

        var act = () => connection.ExecuteSqlAsync(InsertSql, CancellationToken, InsertParameters(short.MaxValue + 1));

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.NumericValueOutOfRange);
    }

    private static string NewEmail() => $"Schema-{Guid.NewGuid():N}@Example.com";

    // InsertSql 매개변수($1 id, $2 email, $3 normalized_email, $4 employee_status). normalized_email은 DB가 강제하지 않으므로 Domain과 같은 ToLowerInvariant 값을 넣는다.
    private static object[] InsertParameters(int status)
    {
        var email = NewEmail();
        return [Guid.NewGuid(), email, email.ToLowerInvariant(), status];
    }

    // UUID v7 앞 48비트 = Unix 밀리초. Guid.ToString은 빅 엔디언 표기라 앞 12자리 16진수가 그 값이다.
    private static long UnixMilliseconds(Guid id) => Convert.ToInt64(id.ToString("N")[..12], 16);

    private static async Task<IReadOnlyList<string>> ReadStringsAsync(NpgsqlConnection connection, string sql)
    {
        var values = new List<string>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
