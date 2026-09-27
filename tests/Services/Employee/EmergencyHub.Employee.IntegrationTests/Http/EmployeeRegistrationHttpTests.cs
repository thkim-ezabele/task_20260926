using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S03-T07 HTTP 인수(FR-08 "등록 → 조회가 HTTP로 동작", "이메일 중복 → 충돌 응답"): 실제 Api 파이프라인(EmployeeApiFactory) + 컨테이너 DB.
// 단위 테스트(Controller CreatedAtRouteResult, Handler 사전 검사, Validator 경계)와 영속성 통합(UnitOfWorkConflictTests 23505 매핑)이 이미 본 규칙은
// 여기서 다시 나누지 않고, HTTP 경계에서만 드러나는 값(실제 Location, JSON 본문, 경합 경로 로그 순서)만 확인한다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-08")]
public sealed class EmployeeRegistrationHttpTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string EmployeesPath = "/api/v1/employees";
    private const string DuplicateEmailMessage = "이미 등록된 이메일입니다.";

    // ---- 성공 ----

    [Fact]
    public async Task PostThenGet_ValidRequest_Returns201WithLocationOfGetRouteAnd200WithNormalizedEmail()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var created = await client.PostAsJsonAsync(
            EmployeesPath,
            new { displayName = "  홍길동  ", email = "  Hong.GilDong@Example.COM ", employeeStatus = 1 },
            CancellationToken);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var id = (await created.Content.ReadFromJsonAsync<CreatedBody>(CancellationToken))!.Id;
        id.Should().NotBeEmpty();
        UuidVersion(id).Should().Be(7, "Handler가 UUID v7을 만든다(ADR-0013)");
        created.Headers.Location.Should().NotBeNull();
        created.Headers.Location!.AbsolutePath.Should().Be($"{EmployeesPath}/{id}", "Location은 GET 라우트(GetEmployeeById)와 같은 경로다");

        using var fetched = await client.GetAsync(created.Headers.Location, CancellationToken);

        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await fetched.Content.ReadFromJsonAsync<EmployeeBody>(CancellationToken);
        body.Should().BeEquivalentTo(new { Id = id, DisplayName = "홍길동", Email = "hong.gildong@example.com", EmployeeStatus = 1 });
        body!.CreatedAt.Offset.Should().Be(TimeSpan.Zero, "감사 시각은 UTC(+00:00)로 응답한다");
        body.UpdatedAt.Should().Be(body.CreatedAt);
    }

    // ---- 실패 ----

    [Theory]
    [InlineData("0192a1b3-0000-7000-8000-000000000001")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Get_UnknownOrEmptyId_Returns404With22001ProblemDetails(string id)
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri($"{EmployeesPath}/{id}", UriKind.Relative), CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, 22001, "직원을 찾을 수 없습니다.", $"{EmployeesPath}/{id}", CancellationToken);
    }

    [Fact]
    public async Task Post_SameEmailAfterTrimAndLowercase_Returns409With23001AndKeepsFirstRowOnly()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var first = await client.PostAsJsonAsync(EmployeesPath, new { displayName = "First", email = " A@X.example.com ", employeeStatus = 1 }, CancellationToken);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = await client.PostAsJsonAsync(EmployeesPath, new { displayName = "Second", email = "a@x.example.com", employeeStatus = 2 }, CancellationToken);

        await second.ShouldBeProblemAsync(HttpStatusCode.Conflict, 23001, DuplicateEmailMessage, EmployeesPath, CancellationToken);
        second.Headers.Location.Should().BeNull();
        (await CountEmployeesAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ConcurrentInsertBetweenPreCheckAndInsert_Returns409With23001WithoutEfErrorAnd102After20001()
    {
        // 사전 검사(SELECT)는 통과하고 INSERT 직전에 다른 연결이 같은 정규화 이메일을 커밋한다 → ux_employees_email 23505 → UoW가 23001로 변환.
        // 병렬 POST 대신 인터셉터로 순서를 고정한다(S03 handoff "병렬 POST 금지").
        const string RaceEmail = "race@example.com";
        var racer = new ConcurrentEmailInsertInterceptor(Database.WriteConnectionString, RaceEmail);
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteInterceptors = [racer] });
        using var client = factory.CreateClient();
        factory.Logs.Clear();

        using var response = await client.PostAsJsonAsync(EmployeesPath, new { displayName = "Racer", email = "  RACE@example.com", employeeStatus = 1 }, CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, 23001, DuplicateEmailMessage, EmployeesPath, CancellationToken);
        racer.Inserted.Should().Be(1, "경합 행은 INSERT 직전에 한 번만 넣는다");
        (await CountEmployeesAsync()).Should().Be(1, "경합으로 들어간 행만 남고 요청의 INSERT는 롤백된다");

        var events = factory.Logs.Events;
        events.Where(e => e.Level >= LogEventLevel.Error).Should().BeEmpty("경합 23505 → 23001은 예상된 실패라 EF 범주 · 요청 완료 로그 모두 Error가 아니다(BL-023)");
        var registered = IndexOfEventId(events, 20001);
        var failed = IndexOfEventId(events, 102);
        registered.Should().BeGreaterThanOrEqualTo(0, "20001은 커밋 전에 남는다(error-codes.md)");
        failed.Should().BeGreaterThan(registered, "102(CommandFailed, 23001)는 20001 뒤에 남는다");
        events[failed].Properties["ErrorCode"].ToString().Should().Be("23001");
        factory.Logs.RequestCompletions.Should().ContainSingle().Which.Properties["StatusCode"].ToString().Should().Be("409");
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData(50, HttpStatusCode.Created)]
    [InlineData(51, HttpStatusCode.BadRequest)]
    public async Task Post_DisplayNameOfEmojis_Accepts50AndRejects51With21002(int emojiCount, HttpStatusCode expected)
    {
        // 이모지 한 글자 = UTF-16 2자 → 50개 = 100자(상한), 51개 = 102자.
        var displayName = string.Concat(Enumerable.Repeat("😀", emojiCount));
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(EmployeesPath, new { displayName, email = "emoji@example.com", employeeStatus = 1 }, CancellationToken);

        response.StatusCode.Should().Be(expected);
        if (expected == HttpStatusCode.Created)
        {
            using var fetched = await client.GetAsync(response.Headers.Location, CancellationToken);
            (await fetched.Content.ReadFromJsonAsync<EmployeeBody>(CancellationToken))!.DisplayName.Should().Be(displayName, "서로게이트 쌍이 깨지지 않고 왕복한다");
            return;
        }

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, "요청 값이 올바르지 않습니다.", EmployeesPath, CancellationToken);
        problem.FieldCodes().Should().Equal([("displayName", 21002)]);
    }

    [Fact]
    public async Task Post_Email254CharsAfterTrimWithSurroundingSpacesAndUppercase_Returns201AndStoresNormalized()
    {
        const string Domain = "@example.com";
        var local = new string('A', 254 - Domain.Length);
        var raw = $"   {local}{Domain}  ";
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(EmployeesPath, new { displayName = "Long Email", email = raw, employeeStatus = 1 }, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "앞뒤 공백을 지운 254자는 상한 이내다");
        using var fetched = await client.GetAsync(response.Headers.Location, CancellationToken);
        var email = (await fetched.Content.ReadFromJsonAsync<EmployeeBody>(CancellationToken))!.Email;
        email.Should().HaveLength(254).And.Be((local + Domain).ToLowerInvariant());
    }

    private static int IndexOfEventId(IReadOnlyList<LogEvent> events, int eventId)
    {
        for (var index = 0; index < events.Count; index++)
        {
            if (events[index].EventId() == eventId)
            {
                return index;
            }
        }

        return -1;
    }

    private static int UuidVersion(Guid id) => Convert.ToInt32(id.ToString("N")[12].ToString(), 16);

    private async Task<long> CountEmployeesAsync()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await using var command = new NpgsqlCommand("SELECT count(*) FROM employees", connection);
        return (long)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private sealed record CreatedBody(Guid Id);

    private sealed record EmployeeBody(Guid Id, string DisplayName, string Email, int EmployeeStatus, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

    /// <summary>요청의 <c>INSERT INTO employees</c> 직전에 다른 연결(자동 커밋)로 같은 이메일 행을 한 번 넣는 쓰기 인터셉터입니다(경합 재현).</summary>
    private sealed class ConcurrentEmailInsertInterceptor(string connectionString, string email) : DbCommandInterceptor
    {
        private int _inserted;

        public int Inserted => Volatile.Read(ref _inserted);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO employees", StringComparison.Ordinal) && Interlocked.CompareExchange(ref _inserted, 1, 0) == 0)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var insert = new NpgsqlCommand(
                    "INSERT INTO employees (id, display_name, email, employee_status, created_at, updated_at) VALUES ($1, 'Racer Winner', $2, 1, now(), now())",
                    connection);
                insert.Parameters.Add(new NpgsqlParameter { Value = Guid.NewGuid() });
                insert.Parameters.Add(new NpgsqlParameter { Value = email });
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            return result;
        }
    }
}
