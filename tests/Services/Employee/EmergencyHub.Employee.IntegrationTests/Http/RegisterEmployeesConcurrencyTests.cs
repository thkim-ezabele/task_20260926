using System.Net;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Npgsql;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S06-T06 완료 조건 ②(PRD-002 FR-06 "동시 요청 경합은 409(행 번호 없음)", FR-10 "동시 요청 23505"):
// (a) 다른 요청의 같은 이메일 — 테스트 호스트 전용 hook(EmailLookupHookRepository, ConfigureTestServices)이 DB 사전 조회 뒤 · 커밋 전에 다른 스코프 · 연결로
//     충돌 행을 커밋한다. 병렬 POST 대신 hook으로 순서를 고정한다(S03 handoff "병렬 POST 금지"). 요청 INSERT → ux 23505 → 23001(행 번호 없음).
// (b) 같은 ID 재전송 — 고정 IIdGenerator(ScriptedIdGenerator)를 Rewind해 두 번째 요청이 첫 요청과 같은 ID를 받는다 → pk 23505 → 매핑 없음 3003 · 로그 202(TD-010).
// 이전(S05 증빙 대응표 EmployeeRegistrationHttpTests.Post_ConcurrentInsertBetweenPreCheckAndInsert_Returns409With23001WithoutEfErrorAnd102After20001).
// 로그 판정: 테스트가 일부러 낸 로그(23505 매핑 201 Debug · EF 실패 로그 Debug, 202 Warning)는 이벤트 ID를 단언하고, 그 밖의 Error 이상은 제외하지 않는다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-06")]
[Trait("FR", "PRD-002/FR-10")]
public sealed class RegisterEmployeesConcurrencyTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string DuplicateEmail = "이미 등록된 이메일입니다.";

    // ---- 성공: hook이 겹치지 않는 행을 먼저 커밋하면 두 쪽 모두 남는다(경합 hook 자체가 요청을 깨지 않음) ----

    [Fact]
    public async Task Post_OtherRequestCommitsDifferentEmailBeforeCommit_Returns201AndBothCommitsRemain()
    {
        var other = new EmployeeBuilder().WithEmail("other@example.com").Build();
        EmployeeApiFactory? factory = null;
        await using var created = factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            AfterEmailLookup = async (_, cancellationToken) =>
                (await EmployeeCommits.AddAndCommitAsync(factory!.Services, other, cancellationToken)).IsSuccess.Should().BeTrue(),
        });
        using var client = factory.CreateClient();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(2, "ok"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await EmployeeRows.ListAsync(Database, CancellationToken)).Select(row => row.NormalizedEmail)
            .Should().BeEquivalentTo(["other@example.com", EmployeeImportData.Email(0, "ok"), EmployeeImportData.Email(1, "ok")]);
    }

    // ---- 실패 (a): 사전 조회 뒤 커밋 전에 같은 이메일이 먼저 커밋됨 → 409 · 23001 · 행 번호 없음, 먼저 커밋된 쪽만 남음 ----

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Post_OtherRequestCommitsSameEmailAfterLookupBeforeCommit_Returns409With23001WithoutRowNumbersAndKeepsFirstCommitOnly(int conflictingRow)
    {
        var raceEmail = EmployeeImportData.Email(conflictingRow, "race");
        var winner = new EmployeeBuilder().WithEmail(raceEmail.ToUpperInvariant()).Build();
        var lookups = new List<IReadOnlyCollection<string>>();
        EmployeeApiFactory? factory = null;
        await using var created = factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            AfterEmailLookup = async (emails, cancellationToken) =>
            {
                lookups.Add([.. emails]);
                (await EmployeeCommits.AddAndCommitAsync(factory!.Services, winner, cancellationToken)).IsSuccess.Should().BeTrue();
            },
        });
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(3, "race"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, 23001, DuplicateEmail, ImportContent.RegisterPath, CancellationToken, hasErrors: false);
        lookups.Should().ContainSingle("사전 조회는 한 번(Handler 1회 실행, 23505는 재시도하지 않음)").Which.Should().Contain(raceEmail);
        (await EmployeeRows.ListAsync(Database, CancellationToken)).Should().Equal(
            [(winner.Id.Value, raceEmail)], "먼저 커밋된 행만 남고 요청의 세 행은 모두 롤백된다");

        var events = factory.Logs.Events;
        events.UnexpectedErrors().Should().BeEmpty("경합 23505 → 23001은 예상된 실패라 EF 범주 · 요청 완료 로그 모두 Error가 아니다(BL-023)");
        var mapped = events.Should().ContainSingle(logEvent => logEvent.EventId() == 201, "매핑된 23505는 201(Debug)").Which;
        mapped.Level.Should().Be(LogEventLevel.Debug);
        mapped.Properties["ConstraintName"].ToString().Should().Be("\"ux_employees_normalized_email\"");
        mapped.Properties["ErrorCode"].ToString().Should().Be("23001");
        var registered = IndexOf(events, 20001);
        var failed = IndexOf(events, 102);
        registered.Should().BeGreaterThanOrEqualTo(0, "20001은 커밋 전에 남는다");
        failed.Should().BeGreaterThan(registered, "102(CommandFailed, 23001)는 20001 뒤에 남는다");
        events[failed].Properties["ErrorCode"].ToString().Should().Be("23001");
        factory.Logs.RequestCompletions.Should().ContainSingle().Which.Properties["StatusCode"].ToString().Should().Be("409");
        PostgresDetails(events).Should().NotContain(detail => detail.Contains(raceEmail, StringComparison.OrdinalIgnoreCase), "Include Error Detail이 없어 23505 Detail 값은 가려진다");
    }

    // ---- 엣지 (b): 같은 ID 재전송 → pk 23505(매핑 없음) → 409 · 3003 · 로그 202(TD-010), 첫 요청 행만 남음 ----

    [Fact]
    public async Task Post_ResendWithSameIds_Returns409With3003AndLogs202AndKeepsFirstRequestRowsOnly()
    {
        var ids = ScriptedIdGenerator.WithRandomIds(3);
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { IdGenerator = ids });
        using var client = factory.CreateClient();
        using (var first = ImportContent.Raw(EmployeeImportData.Csv(3, "first"), "text/csv"))
        {
            (await client.PostAsync(ImportContent.RegisterUri, first, CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var stored = await EmployeeRows.ListAsync(Database, CancellationToken);
        ids.Rewind();
        factory.Logs.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(3, "second"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        await response.ShouldBeProblemAsync(
            HttpStatusCode.Conflict, 3003, CommonErrors.UniqueConstraintViolated.Message, ImportContent.RegisterPath, CancellationToken, hasErrors: false);
        ids.Issued.Should().Be(3, "Handler는 한 번만 실행된다");
        (await EmployeeRows.ListAsync(Database, CancellationToken)).Should().Equal(stored, "두 번째 요청의 행은 하나도 남지 않는다");

        var events = factory.Logs.Events;
        var unmapped = events.Should().ContainSingle(logEvent => logEvent.EventId() == 202).Which;
        unmapped.Level.Should().Be(LogEventLevel.Warning, "매핑 없는 23505는 매핑 누락 또는 TD-010 신호");
        unmapped.Properties["ConstraintName"].ToString().Should().Be("\"pk_employees\"");
        unmapped.Properties["SqlState"].ToString().Should().Be("\"23505\"");
        unmapped.Properties["ErrorCode"].ToString().Should().Be("3003");
        events.UnexpectedErrors().Should().BeEmpty();
        factory.Logs.RequestCompletions.Should().ContainSingle().Which.Properties["StatusCode"].ToString().Should().Be("409");
    }

    private static int IndexOf(IReadOnlyList<LogEvent> events, int eventId)
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

    // 수집한 로그 예외 사슬의 PostgresException.Detail(23505 'Key (…)=(…)' 자리)입니다(reviewer 메모: Dump는 Exception.ToString만 본다).
    private static List<string> PostgresDetails(IEnumerable<LogEvent> events)
    {
        var details = new List<string>();
        foreach (var logEvent in events)
        {
            for (var exception = logEvent.Exception; exception is not null; exception = exception.InnerException)
            {
                if (exception is PostgresException { Detail: { } detail })
                {
                    details.Add(detail);
                }
            }
        }

        return details;
    }
}
