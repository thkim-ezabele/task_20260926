using System.Net;
using System.Text;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.Observability;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S06-T06 완료 조건 ⑤(PRD-002 NFR-04 · FR-10 개인정보 테스트, BL-024 · BL-130): 400 / 409 / 500 응답 본문과 캡처한 로그(Serilog 수집 싱크)에
// 입력한 이름 · 이메일 · 전화번호 값이 없다. 이메일은 전체 · 로컬 부분 · 도메인 부분을 따로 찾는다(부분 노출도 잡음, reviewer 메모 1).
// 경로: 로깅 데코레이터(102), FluentValidation({PropertyValue} 없음), 파서 오류, 23505 detail(Include Error Detail 없음), 전역 예외 처리기(메시지 제거).
// BL-024: ActivityListener(NpgsqlActivityCollector)로 모은 Npgsql span 태그 · 이벤트 · 상태 설명에 DB 비밀번호와 입력 값이 없다(대시보드 수동 확인은 S07-T04).
// 환경은 Development(EF Database.Command Information → SQL 문이 로그에 남는 가장 넓은 설정, EnableSensitiveDataLogging 끔)이다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-10")]
[Trait("NFR", "PRD-002/NFR-04")]
public sealed class RegisterEmployeesPersonalDataTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // 행 오류를 일으키는 값(이름 제어 문자 · 이메일 형식 · 전화 문자)과 정상 값이 섞인 CSV. 행 번호 · 필드 · 정수 코드로만 식별되어야 한다.
    private static readonly string[][] InvalidRows =
    [
        ["홍\u0001길동", "hong.gildong@corp-secret.example", "010-1111-2222", "2000-01-01"],
        ["김비밀", "kim.secret.at.corp-secret.example", "010-3333-4444", "2000-01-02"],
        ["이비밀", "lee.secret@corp-secret.example", "010-55x5-6666", "2000-01-03"],
    ];

    // ---- 성공: 201 요청의 로그에는 직원 ID만 있고 입력 값이 없다 ----

    [Fact]
    public async Task Post_Created_LogsContainNoInputValues()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(3, "ok"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        factory.Logs.Events.Where(logEvent => logEvent.EventId() == 20001).Should().HaveCount(3, "등록 로그는 행마다 ID만 남긴다");
        PersonalDataScan.FindInLogs(factory.Logs.Events, Expand(EmployeeImportData.PersonalValues(3, "ok"))).Should().BeEmpty();
        factory.Logs.Events.UnexpectedErrors().Should().BeEmpty();
    }

    // ---- 실패: 400(행 오류 · 요청 안 중복 · 파서 오류) ----

    [Theory]
    [InlineData("row-errors")]
    [InlineData("duplicate-in-request")]
    [InlineData("json-syntax")]
    [InlineData("json-value-not-string")]
    public async Task Post_InvalidInputReturning400_BodyAndLogsContainNoInputValues(string kind)
    {
        var (bytes, contentType, values) = Invalid400Input(kind);
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var content = ImportContent.Raw(bytes, contentType);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await ShouldNotExposeAsync(factory, response, values);
        factory.Logs.Events.Should().Contain(logEvent => logEvent.EventId() == 102, "로깅 데코레이터 실패 로그(102) 경로를 지난다");
        factory.Logs.Events.UnexpectedErrors().Should().BeEmpty();
    }

    // ---- 실패: 409(DB 기존 이메일 · 경합 23505) ----

    [Fact]
    public async Task Post_409StoredEmail_BodyAndLogsContainNoInputValues()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using (var first = ImportContent.Raw(EmployeeImportData.Csv(2, "dup"), "text/csv"))
        {
            (await client.PostAsync(ImportContent.RegisterUri, first, CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        factory.Logs.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Json(2, "dup"), "application/json");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await ShouldNotExposeAsync(factory, response, EmployeeImportData.PersonalValues(2, "dup"));
        factory.Logs.Events.UnexpectedErrors().Should().BeEmpty();
    }

    [Fact]
    public async Task Post_409UniqueViolationRace_BodyLogsAndSpansContainNoInputValuesOrPassword()
    {
        EmployeeApiFactory? factory = null;
        await using var created = factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            AfterEmailLookup = async (_, cancellationToken) =>
                (await EmployeeCommits.AddAndCommitAsync(
                    factory!.Services, new EmployeeBuilder().WithEmail(EmployeeImportData.Email(0, "race")).Build(), cancellationToken)).IsSuccess.Should().BeTrue(),
        });
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var spans = new NpgsqlActivityCollector();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(2, "race"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var values = EmployeeImportData.PersonalValues(2, "race");
        await ShouldNotExposeAsync(factory, response, values);
        factory.Logs.Events.Should().ContainSingle(logEvent => logEvent.EventId() == 201, "23505 매핑 로그(Debug) 경로를 지난다");
        factory.Logs.Events.UnexpectedErrors().Should().BeEmpty();
        spans.Activities.Should().Contain(
            span => span.Tag("otel.status_code") == "ERROR" && span.Events.Any(e => e.Contains("Npgsql.PostgresException", StringComparison.Ordinal)),
            "23505로 실패한 INSERT span(exception 이벤트 · 스택 포함)도 검사 대상이다");
        SpansShouldNotExpose(spans, values);
    }

    // ---- 실패: 500(예외 메시지에 값이 든 예외 · 읽기 전용 연결로 쓰기) ----

    [Fact]
    public async Task Post_500ExceptionMessageCarryingInputValues_BodyAndLogsContainNoInputValues()
    {
        var values = EmployeeImportData.PersonalValues(2, "boom");
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            AfterEmailLookup = (_, _) => throw new InvalidOperationException("행 값 " + string.Join(", ", values)),
        });
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(2, "boom"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        await ShouldNotExposeAsync(factory, response, values);
        factory.Logs.Events.UnexpectedErrors(1).Should().ContainSingle("이벤트 1(전역 예외) 밖의 Error는 요청 완료 로그 한 줄뿐")
            .Which.MessageTemplate.Text.Should().Be(SerilogEventCollector.RequestCompletionTemplate);
    }

    [Fact]
    public async Task Post_500ReadOnlyWriteConnection_BodyLogsAndSpansContainNoInputValuesOrPassword()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { WriteConnectionString = Database.ReadConnectionString });
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var spans = new NpgsqlActivityCollector();
        using var content = ImportContent.MultipartFile(EmployeeImportData.Json(2, "ro"), "employees.json");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var values = EmployeeImportData.PersonalValues(2, "ro");
        await ShouldNotExposeAsync(factory, response, values);
        SpansShouldNotExpose(spans, values);
        factory.Logs.Events.UnexpectedErrors(1).Should().ContainSingle("이벤트 1(전역 예외) 밖의 Error는 요청 완료 로그 한 줄뿐")
            .Which.MessageTemplate.Text.Should().Be(SerilogEventCollector.RequestCompletionTemplate);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    // ---- 엣지: BL-024 성공 요청 span(사전 조회 = ANY 배열 매개변수 · 1,000행 INSERT 배치)에 비밀번호 · 입력 값 없음 ----

    [Fact]
    public async Task Post_Created1000Rows_NpgsqlSpanTagsContainNoPasswordOrInputValues()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var spans = new NpgsqlActivityCollector();
        using var content = ImportContent.Raw(EmployeeImportData.Csv(1000, "span"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        spans.Activities.Should().Contain(span => span.Tag("db.statement") != null && span.Tag("db.statement")!.Contains("INSERT INTO", StringComparison.Ordinal));
        spans.Activities.Should().Contain(span => span.Tag("db.statement") != null && span.Tag("db.statement")!.Contains("ANY", StringComparison.Ordinal));
        SpansShouldNotExpose(spans, EmployeeImportData.PersonalValues(1000, "span"));
    }

    private static (byte[] Bytes, string ContentType, IReadOnlyList<string> Values) Invalid400Input(string kind)
    {
        switch (kind)
        {
            case "row-errors":
                return (
                    Encoding.UTF8.GetBytes(string.Concat(InvalidRows.Select(row => string.Join(',', row) + "\n"))),
                    "text/csv",
                    [.. InvalidRows.SelectMany(row => row[..3])]);
            case "duplicate-in-request":
                var csv = "최비밀,choi.secret@corp-secret.example,010-7777-8888,2000-01-01\n정비밀,CHOI.SECRET@corp-secret.example,010-9999-0000,2000-01-02\n";
                return (Encoding.UTF8.GetBytes(csv), "text/csv", ["최비밀", "choi.secret@corp-secret.example", "010-7777-8888", "정비밀", "010-9999-0000"]);
            case "json-syntax":
                return (
                    Encoding.UTF8.GetBytes("""[{"name":"한비밀","email":"han.secret@corp-secret.example","tel":"010-1212-3434" "joined":"2000-01-01"}]"""),
                    "application/json",
                    ["한비밀", "han.secret@corp-secret.example", "010-1212-3434"]);
            default:
                return (
                    Encoding.UTF8.GetBytes("""[{"name":"오비밀","email":"oh.secret@corp-secret.example","tel":1012345678,"joined":"2000-01-01"}]"""),
                    "application/json",
                    ["오비밀", "oh.secret@corp-secret.example", "1012345678"]);
        }
    }

    // 이메일은 전체 값에 더해 로컬 부분 · 도메인 부분도 찾는다(부분 문자열 노출).
    private static List<string> Expand(IEnumerable<string> values) =>
        [.. values.SelectMany(value => value.Contains('@', StringComparison.Ordinal) ? [value, .. value.Split('@')] : new[] { value })];

    private async Task ShouldNotExposeAsync(EmployeeApiFactory factory, HttpResponseMessage response, IEnumerable<string> values)
    {
        var searched = Expand(values);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        PersonalDataScan.FindInJson(body, searched).Should().BeEmpty("응답 본문(detail · errors 포함)은 행 번호 · 필드 · 정수 코드로만 식별한다");
        PersonalDataScan.FindInLogs(factory.Logs.Events, searched).Should().BeEmpty("로그에 입력 값을 남기지 않는다(NFR-04)");
        factory.Logs.Events.Should().NotBeEmpty("로그를 실제로 모았는지(빈 수집으로 통과하지 않음)");
    }

    private void SpansShouldNotExpose(NpgsqlActivityCollector spans, IEnumerable<string> values)
    {
        var passwords = new[] { Database.WriteConnectionString, Database.ReadConnectionString }
            .Select(connectionString => new NpgsqlConnectionStringBuilder(connectionString).Password)
            .OfType<string>()
            .ToList();
        passwords.Should().NotBeEmpty().And.AllSatisfy(password => password.Should().NotBeNullOrEmpty());
        var activities = spans.Activities;
        activities.Should().NotBeEmpty("span을 실제로 모았는지(빈 수집으로 통과하지 않음)");
        foreach (var activity in activities)
        {
            var dump = activity.Dump();
            PersonalDataScan.FindIn(dump, passwords).Should().BeEmpty("span에 DB 비밀번호가 없다(BL-024)");
            PersonalDataScan.FindIn(dump, Expand(values)).Should().BeEmpty("span에 SQL 파라미터 값(입력 값)이 없다(BL-024)");
            (activity.Tag("db.connection_string") ?? string.Empty).Should().NotContainEquivalentOf("password=");
        }
    }
}
