using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace EmergencyHub.Employee.IntegrationTests.Persistence;

// FR-06 공통 설정의 실제 DB 왕복(S03-T02 tester 인계, BL-082 S1 · testing-strategy.md Q8):
// 추가 → 커밋 → 새 쓰기 DbContext로 다시 읽기(private 생성자 구체화, 강타입 ID 변환, 감사 UTC, xmin 채움)와
// 읽기 DbContext 프로젝션(e.Id == new EmployeeId(id) · e.Id.Value · VO .Value 최상위 프로젝션, shadow 감사 시각)을 실제 PostgreSQL에서 한 번 실행한다.
// S05-T04: PRD-001 샘플 조회(IEmployeeReadRepository.GetByIdAsync)를 지워, 읽기 쪽 확인은 읽기 DbContext에 직접 쓴 프로젝션으로 바꿨다
// (Read Repository 목록 · 이름 조회는 S05-T06). 새 스키마(InitialCreate 재생성)는 S05-T05에서 적용되므로 그 전에는 실패 허용 목록이다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-06")]
public sealed class EmployeePersistenceRoundTripTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 10, 15, 30, 123, TimeSpan.Zero);

    // 서울 2027-01-01 08:30:00.456 = UTC 2026-12-31 23:30:00.456(연말 · 날짜 경계를 넘는 오프셋).
    private static readonly DateTimeOffset SeoulNewYearMorning = new(2027, 1, 1, 8, 30, 0, 456, TimeSpan.FromHours(9));

    // ---- 성공 ----

    [Fact]
    public async Task CommitThenReload_NewWriteDbContext_MaterializesThroughPrivateConstructorWithAuditAndVersion()
    {
        await using var services = Database.CreateServices(new EmployeeServicesOptions { TimeProvider = new FakeTimeProvider(Now) });
        var employee = new EmployeeBuilder().WithName("홍길동").WithEmail("Round.Trip@Example.com").WithPhoneNumber("02-123-4567").WithJoinedOn("1999-12-31").Build();
        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();

        var loaded = await db.Set<Domain.Employees.Employee>().SingleAsync(e => e.Id == employee.Id, CancellationToken);

        employee.DomainEvents.Should().BeEmpty("커밋 성공 뒤 UnitOfWork가 이벤트를 비운다");
        (loaded.Id, loaded.Name.Value, loaded.Email.Value, loaded.NormalizedEmail, loaded.PhoneNumber.Value, loaded.JoinedOn.Value, loaded.EmployeeStatus)
            .Should().Be((employee.Id, "홍길동", "Round.Trip@Example.com", "round.trip@example.com", "02-123-4567", new DateOnly(1999, 12, 31), EmployeeStatus.Active));
        loaded.DomainEvents.Should().BeEmpty("구체화는 Register가 아니라 private 생성자를 쓴다");
        var entry = db.Entry(loaded);
        entry.Property<DateTimeOffset>(ShadowPropertyNames.CreatedAt).CurrentValue.Should().Be(Now).And.HaveOffset(TimeSpan.Zero);
        entry.Property<DateTimeOffset>(ShadowPropertyNames.UpdatedAt).CurrentValue.Should().Be(Now).And.HaveOffset(TimeSpan.Zero);
        entry.Property<uint>(ShadowPropertyNames.Version).CurrentValue.Should().Be(await ReadXminAsync(employee.Id.Value));
    }

    [Fact]
    public async Task ReadContext_StoredEmployee_ProjectsIdValueValueObjectsAndShadowAuditColumns()
    {
        await using var services = Database.CreateServices(new EmployeeServicesOptions { TimeProvider = new FakeTimeProvider(Now) });
        var employee = new EmployeeBuilder().WithEmail("Read.Side@Example.com").WithStatus(EmployeeStatus.Inactive).Build();
        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();

        var stored = await ReadOnReadConnectionAsync(scope.ServiceProvider, employee.Id.Value);

        stored.Should().Be(new StoredEmployee(
            employee.Id.Value,
            EmployeeBuilder.DefaultName,
            "Read.Side@Example.com",
            "read.side@example.com",
            EmployeeBuilder.DefaultPhoneNumber,
            new DateOnly(2020, 3, 2),
            EmployeeStatus.Inactive,
            Now,
            Now));
        stored!.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    // ---- 실패 ----

    [Fact]
    public async Task ReadContext_UnknownOrEmptyId_ProjectsNothing()
    {
        await using var services = Database.CreateServices();
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();

        var results = new[]
        {
            await ReadOnReadConnectionAsync(scope.ServiceProvider, Guid.NewGuid()),
            await ReadOnReadConnectionAsync(scope.ServiceProvider, Guid.Empty),
        };

        results.Should().AllSatisfy(result => result.Should().BeNull());
    }

    // ---- 엣지 ----

    [Theory]
    [MemberData(nameof(BoundaryValues))]
    public async Task CommitThenRead_MaxLengthAndUnicodeValues_RoundTripUnchanged(string name, string email, string phoneNumber)
    {
        // 도메인 길이(UTF-16 코드 단위) 이하면 DB varchar(n)(문자 수)에도 들어간다. 이모지는 2단위 = 1문자.
        await using var services = Database.CreateServices();
        var employee = new EmployeeBuilder().WithName(name).WithEmail(email).WithPhoneNumber(phoneNumber).Build();
        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();

        var stored = await ReadOnReadConnectionAsync(scope.ServiceProvider, employee.Id.Value);

        (stored!.Name, stored.Email, stored.NormalizedEmail, stored.PhoneNumber).Should().Be((name, email, email.ToLowerInvariant(), phoneNumber));
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-01")]
    public async Task Commit_NfdName_StoresNfcCodePointsInNameColumn()
    {
        // S05-T06 완료 조건 ③: NFD(자모 분리)로 들어온 이름은 Name.Create가 NFC로 바꿔 저장한다. DB 값이 NFC 문자열과 바이트까지 같고,
        // 서버의 NFC 판정(IS NFC NORMALIZED)도 참이며 자모 분리 형태로는 찾을 수 없다.
        await using var services = Database.CreateServices();
        var nfd = "홍길동".Normalize(System.Text.NormalizationForm.FormD);
        nfd.Length.Should().BeGreaterThan(3, "NFD 입력은 자모 분리 형태여야 한다");
        var employee = new EmployeeBuilder().WithName(nfd).Build();

        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();

        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<string>("SELECT name FROM employees WHERE id = $1", CancellationToken, employee.Id.Value)).Should().Be("홍길동");
        (await connection.ScalarAsync<int>("SELECT char_length(name) FROM employees WHERE id = $1", CancellationToken, employee.Id.Value)).Should().Be(3);
        (await connection.ScalarAsync<bool>("SELECT name IS NFC NORMALIZED FROM employees WHERE id = $1", CancellationToken, employee.Id.Value)).Should().BeTrue();
        (await connection.ScalarAsync<long>("SELECT count(*) FROM employees WHERE name = $1", CancellationToken, nfd)).Should().Be(0);
    }

    [Theory]
    [Trait("FR", "PRD-002/FR-01")]
    [InlineData("1900-01-01")]
    [InlineData("2999-12-31")]
    public async Task CommitThenRawRead_JoinedOnLowerBoundOrFutureDate_StoresDateUnchangedAndActiveStatusAsOne(string joinedOn)
    {
        // S05-T06 tester 보강: joined_on 하한(1900-01-01, 포함)과 미래 날짜(허용)는 DB date 컬럼에 날짜 그대로 저장되고(시간대 이동 없음),
        // 등록 상태 Active는 employee_status smallint 1로 저장된다(ADR-0008). 읽기 Repository 목록 프로젝션에서도 같은 날짜로 돌아온다.
        await using var services = Database.CreateServices();
        var employee = new EmployeeBuilder().WithJoinedOn(joinedOn).Build();

        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();

        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<string>("SELECT joined_on::text FROM employees WHERE id = $1", CancellationToken, employee.Id.Value)).Should().Be(joinedOn);
        (await connection.ScalarAsync<short>("SELECT employee_status FROM employees WHERE id = $1", CancellationToken, employee.Id.Value)).Should().Be((short)1);
        await using var scope = services.CreateAsyncScope();
        var listed = await scope.ServiceProvider.GetRequiredService<Application.Employees.IEmployeeReadRepository>().ListOrderedByJoinedOnAsync(0, 10, CancellationToken);
        listed.Should().ContainSingle().Which.JoinedOn.Should().Be(DateOnly.ParseExact(joinedOn, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Commit_TimeProviderAtPlusNineAndSeoulSession_StoresSameInstantAsUtc()
    {
        // S1: FakeTimeProvider가 +09:00 값을 돌려주고 EF 연결 세션이 Asia/Seoul이어도 timestamptz에는 같은 순간이 저장되고, 읽은 값의 오프셋은 0이다.
        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            TimeProvider = new FakeTimeProvider(SeoulNewYearMorning),
            WriteConnectionString = Database.WriteConnectionStringWith(builder => builder.Timezone = "Asia/Seoul"),
            ReadConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(Database.ReadConnectionString) { Timezone = "Asia/Seoul" }.ConnectionString,
        });
        var employee = new EmployeeBuilder().Build();

        (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();

        var seoul = await ReadCreatedAtInSessionAsync(employee.Id.Value, "Asia/Seoul");
        var utc = await ReadCreatedAtInSessionAsync(employee.Id.Value, "UTC");
        seoul.Epoch.Should().Be(utc.Epoch);
        (seoul.Display, utc.Display).Should().Be(("2027-01-01 08:30:00.456+09", "2026-12-31 23:30:00.456+00"));
        await using var scope = services.CreateAsyncScope();
        var stored = await ReadOnReadConnectionAsync(scope.ServiceProvider, employee.Id.Value);
        stored!.CreatedAt.Should().Be(SeoulNewYearMorning).And.HaveOffset(TimeSpan.Zero);
        stored.CreatedAt.UtcDateTime.Should().Be(new DateTime(2026, 12, 31, 23, 30, 0, 456, DateTimeKind.Utc));
    }

    // 1행: name 100자 · email 254자(대문자 local, 정규화 값은 소문자) · phone 20자(숫자 11 + 하이픈 9, 경계). 2행: 이모지 50개(UTF-16 100).
    public static TheoryData<string, string, string> BoundaryValues() => new()
    {
        { new string('가', Name.MaxLength), $"{new string('A', 64)}@{new string('b', Email.MaxLength - 64 - 1 - ".example.com".Length)}.example.com", "0-1-2-3-4-5-6-7-8-90" },
        { string.Concat(Enumerable.Repeat("😀", Name.MaxLength / 2)), "Emoji@Example.com", "01012345678" },
        { "한글 English 混合 😀", "mixed@example.com", "010-1234-5678" },
    };

    // 읽기 연결(EmployeeReadDbContext)에서 VO는 .Value로, 감사 시각은 shadow property로 최상위 프로젝션한다(S05-T04 번역 실측과 같은 형태).
    private Task<StoredEmployee?> ReadOnReadConnectionAsync(IServiceProvider scopeServices, Guid id) =>
        scopeServices.GetRequiredService<EmployeeReadDbContext>().Set<Domain.Employees.Employee>()
            .Where(employee => employee.Id == new EmployeeId(id))
            .Select(employee => new StoredEmployee(
                employee.Id.Value,
                employee.Name.Value,
                employee.Email.Value,
                employee.NormalizedEmail,
                employee.PhoneNumber.Value,
                employee.JoinedOn.Value,
                employee.EmployeeStatus,
                EF.Property<DateTimeOffset>(employee, ShadowPropertyNames.CreatedAt),
                EF.Property<DateTimeOffset>(employee, ShadowPropertyNames.UpdatedAt)))
            .FirstOrDefaultAsync(CancellationToken);

    private async Task<uint> ReadXminAsync(Guid id)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        return checked((uint)await connection.ScalarAsync<long>("SELECT xmin::text::bigint FROM employees WHERE id = $1", CancellationToken, id));
    }

    private async Task<(string Epoch, string Display)> ReadCreatedAtInSessionAsync(Guid id, string timeZone)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await connection.ExecuteSqlAsync($"SET TIME ZONE '{timeZone}'", CancellationToken);
        var epoch = await connection.ScalarAsync<string>("SELECT extract(epoch FROM created_at)::text FROM employees WHERE id = $1", CancellationToken, id);
        var display = await connection.ScalarAsync<string>("SELECT created_at::text FROM employees WHERE id = $1", CancellationToken, id);
        return (epoch, display);
    }

    private sealed record StoredEmployee(
        Guid Id,
        string Name,
        string Email,
        string NormalizedEmail,
        string PhoneNumber,
        DateOnly JoinedOn,
        EmployeeStatus EmployeeStatus,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
