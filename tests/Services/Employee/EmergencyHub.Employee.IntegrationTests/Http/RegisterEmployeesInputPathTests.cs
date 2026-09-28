using System.Net;
using System.Text;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S06-T06 완료 조건 ①(PRD-002 FR-05 입력 경로 표 · FR-06 · FR-10 통합): 실제 Api 파이프라인(바인더 · Validator · Handler · UnitOfWork) + 컨테이너 DB.
// Api.UnitTests RegisterEmployeesHttpTests는 ISender 대역이라 Validator · Handler · DB를 지나지 않는다. 여기서는 입력 경로 표의 행마다 최종 상태 코드 · 정수 code와
// "실패 때 DB 0건"을 DB에서 확인한다. 1 MiB 초과 Kestrel 경로는 RegisterEmployeesKestrelLimitTests, 경합은 RegisterEmployeesConcurrencyTests.
// 0행 입력(JSON [], NBSP만 있는 CSV 줄)은 Handler가 400 · 21028로 거부한다(BL-137 결정 A, S07-T05).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-05")]
[Trait("FR", "PRD-002/FR-06")]
[Trait("FR", "PRD-002/FR-10")]
public sealed class RegisterEmployeesInputPathTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string ValidationFailed = "요청 값이 올바르지 않습니다.";
    private const string DuplicateEmail = "이미 등록된 이메일입니다.";

    // ---- 성공: 입력 경로 표 201 행(전 경로 CSV · JSON) → 201 {count, ids}, 입력 순서 ids, DB에 같은 행 ----

    public static TheoryData<string, bool, bool> SupportedPaths()
    {
        var data = new TheoryData<string, bool, bool>();
        foreach (var path in ImportContent.Paths)
        {
            data.Add(path, false, false);
            data.Add(path, true, false);
        }

        data.Add("raw", true, true);
        data.Add("multipart-data", true, true);
        return data;
    }

    [Theory]
    [MemberData(nameof(SupportedPaths))]
    public async Task Post_SupportedInputPath_Returns201WithCountAndIdsInInputOrderAndStoresRows(string path, bool json, bool withoutBrackets)
    {
        var bytes = json ? EmployeeImportData.Json(3, "p") : EmployeeImportData.Csv(3, "p");
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.For(path, withoutBrackets ? ImportContent.WithoutBrackets(bytes) : bytes, json);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().BeNull("ADR-0025: 일괄 201은 Location을 생략한다");
        var ids = await ReadCreatedIdsAsync(response, expectedCount: 3);
        ids.Should().AllSatisfy(id => UuidVersion(id).Should().Be(7, "Handler가 UUID v7을 만든다(ADR-0013)"));
        (await EmployeeRows.ListAsync(Database, CancellationToken)).Should().Equal(
            ids.Select((id, index) => (id, EmployeeImportData.Email(index, "p"))),
            "ids는 입력 순서이고(UUID v7 단조) 행마다 정규화 이메일이 저장된다");
    }

    // 이전(S05 증빙 대응표 EmployeeRegistrationHttpTests.Post_Email254CharsAfterTrimWithSurroundingSpacesAndUppercase_…):
    // 앞뒤 공백을 지운 254자 대문자 이메일은 201, normalized_email은 소문자 · email은 입력 표기(Trim만) 보존.
    [Fact]
    public async Task Post_Email254CharsAfterTrimWithSurroundingSpacesAndUppercase_Returns201AndStoresNormalized()
    {
        const string Domain = "@EXAMPLE.com";
        var email = new string('A', 254 - Domain.Length) + Domain;
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.Raw(Encoding.UTF8.GetBytes($"김이름,   {email}  ,010-0000-0000,2000-01-01\n"), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var stored = await ReadStoredAsync("email, normalized_email");
        stored.Should().Equal([$"{email}|{email.ToLowerInvariant()}"], "email은 입력 표기(Trim만), normalized_email은 소문자");
    }

    // 이전(EmployeeRegistrationHttpTests.Post_DisplayNameOfEmojis_Accepts50AndRejects51With21002): 이모지 한 글자 = UTF-16 2자 → 50개 = 100자(상한) 201,
    // 51개 = 102자 400 · rows[1].name 21008(PRD-002 코드). 서로게이트 쌍이 깨지지 않고 저장된다.
    [Theory]
    [InlineData(50, HttpStatusCode.Created)]
    [InlineData(51, HttpStatusCode.BadRequest)]
    public async Task Post_NameOfEmojis_Accepts50AndRejects51With21008(int emojiCount, HttpStatusCode expected)
    {
        var name = string.Concat(Enumerable.Repeat("😀", emojiCount));
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.MultipartData(JsonRow(name, "emoji@example.com"));

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(expected);
        if (expected == HttpStatusCode.Created)
        {
            (await ReadStoredAsync("name")).Should().Equal([name]);
            return;
        }

        (await ShouldBeValidationProblemAsync(response)).FieldCodes().Should().Equal([("rows[1].name", 21008)]);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    // ---- 실패: 입력 경로 표 400 · 415 행(전송 형식 · 조합 · 요청 전체 오류) → 기대 code, DB 0건 ----

    public static TheoryData<string, string, int> RequestLevelFailures() => new()
    {
        // 입력 경로 표 "form-urlencoded, data 키 없음(curl -d '김이름,...')" → 빈 입력 21028.
        { "form-urlencoded-without-data-key", string.Empty, 21028 },
        { "multipart-file-and-data", string.Empty, 21029 },
        { "multipart-neither-file-nor-data", string.Empty, 21028 },
        { "multipart-file-field-without-filename", string.Empty, 21028 },
        { "multipart-data-field-with-filename", string.Empty, 21028 },
        { "raw-empty-body", string.Empty, 21028 },

        // 행 0개(BL-137 결정 A, S07-T05): Validator 공백 집합 밖이라 Validator는 지나고, 파서가 행 0개로 읽어 Handler가 21028로 거부한다.
        { "raw-json-zero-rows", string.Empty, 21028 },
        { "raw-csv-nbsp-only-lines", string.Empty, 21028 },
        { "raw-csv-1001-rows", string.Empty, 21027 },
        { "multipart-file-json-1001-rows", string.Empty, 21027 },
        { "raw-json-syntax-invalid", string.Empty, 21023 },
    };

    [Theory]
    [MemberData(nameof(RequestLevelFailures))]
    public async Task Post_RequestLevelInvalidInput_Returns400WithSingleEmptyPathCodeAndStoresNothing(string request, string field, int code)
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = RequestLevelContent(request);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        (await ShouldBeValidationProblemAsync(response)).FieldCodes().Should().Equal([(field, code)], "요청 전체 오류는 경로 \"\" 하나만 보고한다");
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    // 잘못된 UTF-8(0xC3 0x28)은 네 입력 경로 모두 21022, 경로 "" 하나(HTTP 전 구간 확인).
    [Theory]
    [InlineData("multipart-file")]
    [InlineData("multipart-data")]
    [InlineData("form-data")]
    [InlineData("raw")]
    public async Task Post_InvalidUtf8OnEveryInputPath_Returns400With21022AndStoresNothing(string path)
    {
        byte[] bytes = [.. Encoding.UTF8.GetBytes("김이름,kim@gmail.com,010-0000-0000,2000-01-01\n이름"), 0xC3, 0x28, .. Encoding.UTF8.GetBytes(",lee@gmail.com,010-0000-0001,2000-01-01\n")];
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.For(path, bytes, json: false);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        (await ShouldBeValidationProblemAsync(response)).FieldCodes().Should().Equal([(string.Empty, 21022)]);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    // 전송 형식 오류(같은 필드 두 번, multipart 헤더 수 · 길이 한도 위반)는 바인더가 거부해 400 · 1001, errors 키 "" 하나(reviewer T05 지적 포함).
    [Theory]
    [InlineData("multipart-data-twice")]
    [InlineData("multipart-file-twice")]
    [InlineData("form-urlencoded-data-twice")]
    [InlineData("multipart-section-with-17-headers")]
    [InlineData("multipart-section-header-over-16-kib")]
    public async Task Post_MalformedTransport_Returns400With1001UnderEmptyKeyAndStoresNothing(string request)
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = MalformedTransportContent(request);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        var problem = await ShouldBeValidationProblemAsync(response);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).Should().Equal(string.Empty);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    // 경계: 섹션 헤더 16개(Content-Disposition + 15, MultipartReader HeadersCountLimit 기본값과 같음)는 받는다.
    [Fact]
    public async Task Post_MultipartSectionWith16Headers_Returns201()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = RawMultipart(string.Concat(Enumerable.Range(1, 15).Select(index => $"X-Test-{index}: v\r\n")));

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(1);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/merge-patch+json")]
    public async Task Post_UnsupportedContentType_Returns415With1005AndStoresNothing(string contentType)
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.Raw(EmployeeImportData.Json(1), contentType);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        await response.ShouldBeProblemAsync(
            HttpStatusCode.UnsupportedMediaType, 1005, CommonErrors.UnsupportedMediaType.Message, ImportContent.RegisterPath, CancellationToken);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    // ---- 실패: 행 오류(FR-06 "한 행 오류가 섞이면 0건 저장 · 400 · 해당 행 번호") ----

    [Fact]
    public async Task Post_OneInvalidRowAmongValidRows_Returns400WithThatRowOnlyAndStoresNothing()
    {
        var csv = "김이름,kim@gmail.com,010-0000-0000,2000-01-01\n이이름,lee@gmail.com,010-abcd-0001,2000-13-01\n박이름,park@gmail.com,010-0000-0002,2000-01-03\n";
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.Raw(Encoding.UTF8.GetBytes(csv), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        (await ShouldBeValidationProblemAsync(response)).FieldCodes().Should().Equal(
            [("rows[2].tel", 21011), ("rows[2].joined", 21016)],
            "한 행 안에서는 필드 순서(name → email → tel → joined), 키는 camelCase");
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    // BL-129: NUL(U+0000)이 든 이메일은 저장 전에 21004로 거부된다(PostgreSQL text에 NUL 저장 불가 → 500이 아님). CSV 바이트 · JSON 이스케이프 둘 다.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_EmailWithNul_Returns400With21004AndStoresNothing(bool json)
    {
        var bytes = json
            ? Encoding.UTF8.GetBytes("""[{"name":"김이름","email":"kim\u0000@gmail.com","tel":"010-0000-0000","joined":"2000-01-01"}]""")
            : [.. Encoding.UTF8.GetBytes("김이름,kim"), 0x00, .. Encoding.UTF8.GetBytes("@gmail.com,010-0000-0000,2000-01-01\n")];
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.Raw(bytes, json ? "application/json" : "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        (await ShouldBeValidationProblemAsync(response)).FieldCodes().Should().Equal([("rows[1].email", 21004)]);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
        factory.Logs.Events.UnexpectedErrors().Should().BeEmpty("NUL은 DB까지 가지 않는다(500 · 9001 경로 아님)");
    }

    [Fact]
    public async Task Post_DuplicateEmailsInRequestDifferingOnlyInCase_Returns400MarkingBothRowsAndStoresNothing()
    {
        var csv = "김이름,kim@gmail.com,010-0000-0000,2000-01-01\n이이름,lee@gmail.com,010-0000-0001,2000-01-02\n박이름, KIM@Gmail.COM ,010-0000-0002,2000-01-03\n";
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.MultipartFile(Encoding.UTF8.GetBytes(csv), "employees.csv", "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        (await ShouldBeValidationProblemAsync(response)).FieldCodes().Should().Equal([("rows[1].email", 21018), ("rows[3].email", 21018)]);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Post_RowErrorAndInRequestDuplicate_ReportsRowErrorOnlyWithout21018()
    {
        var csv = "김이름,kim@gmail.com,010-0000-0000,2000-01-01\n이이름,lee@gmail.com,010-0000-0001,1899-12-31\n박이름,kim@gmail.com,010-0000-0002,2000-01-03\n";
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.Raw(Encoding.UTF8.GetBytes(csv), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        (await ShouldBeValidationProblemAsync(response)).FieldCodes().Should().Equal([("rows[2].joined", 21017)], "앞 단계(행 검증)가 실패하면 요청 안 중복 검사를 하지 않는다");
    }

    // 이전(EmployeeRegistrationHttpTests.Post_SameEmailAfterTrimAndLowercase_Returns409With23001AndKeepsFirstRowOnly):
    // DB에 이미 있는 이메일(Trim · 소문자 기준)은 409 · 23001 + 충돌 행 번호(rows[n].email 소문자 키), 기존 행만 남는다.
    [Fact]
    public async Task Post_EmailAlreadyStoredAfterTrimAndLowercase_Returns409WithConflictRowNumberAndKeepsExistingRowOnly()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using (var first = ImportContent.Raw(Encoding.UTF8.GetBytes("김이름, Kim@Gmail.com ,010-0000-0000,2000-01-01\n"), "text/csv"))
        {
            (await client.PostAsync(ImportContent.RegisterUri, first, CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var existing = await EmployeeRows.ListAsync(Database, CancellationToken);
        using var content = ImportContent.FormData(Encoding.UTF8.GetBytes("이이름,lee@gmail.com,010-0000-0001,2000-01-02\n박이름,KIM@gmail.COM,010-0000-0002,2000-01-03\n"));

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.Conflict, 23001, DuplicateEmail, ImportContent.RegisterPath, CancellationToken, hasErrors: true);
        problem.FieldCodes().Should().Equal([("rows[2].email", 23001)]);
        (await EmployeeRows.ListAsync(Database, CancellationToken)).Should().Equal(existing, "409면 요청의 어느 행도 저장하지 않는다");
    }

    // ---- 엣지: 행 오류 · 충돌 100개 상한과 잘림 표시(101번째 = 경로 "" 21030 · 23002) ----

    [Fact]
    public async Task Post_MoreThan100RowErrors_Reports100ThenTruncationMarker21030()
    {
        var csv = string.Concat(Enumerable.Range(1, 150).Select(index => $"이름{index},not-an-email-{index},010-0000-0000,2000-01-01\n"));
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.Raw(Encoding.UTF8.GetBytes(csv), "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        (await ShouldBeValidationProblemAsync(response)).FieldCodes().Should().Equal(
            [.. Enumerable.Range(1, 100).Select(row => ($"rows[{row}].email", 21004)), (string.Empty, 21030)]);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Post_MoreThan100StoredEmailConflicts_Reports100ThenTruncationMarker23002()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using (var first = ImportContent.Raw(EmployeeImportData.Csv(101, "t"), "text/csv"))
        {
            (await client.PostAsync(ImportContent.RegisterUri, first, CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var content = ImportContent.Raw(EmployeeImportData.Json(101, "t"), "application/json");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.Conflict, 23001, DuplicateEmail, ImportContent.RegisterPath, CancellationToken, hasErrors: true);
        problem.FieldCodes().Should().Equal([.. Enumerable.Range(1, 100).Select(row => ($"rows[{row}].email", 23001)), (string.Empty, 23002)]);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(101);
    }

    [Fact]
    public async Task Post_Exactly1000Rows_Returns201AndStores1000()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.MultipartFile(EmployeeImportData.Json(1000), "employees.json");

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadCreatedIdsAsync(response, expectedCount: 1000)).Should().OnlyHaveUniqueItems();
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(1000);
    }

    // 이전(ProblemDetailsHttpTests.Post_TraceparentHeader_ProblemTraceIdEqualsIncomingTraceId): 실패 응답 traceId와 요청 완료 로그가 들어온 trace-id를 잇는다.
    // 이전(ProblemDetailsHttpTests.Post_EmptyBody_… · Post_UndefinedEmployeeStatus_…의 HTTP 400 · 1001 형식): 21028 경로의 전체 ProblemDetails 형태.
    [Fact]
    public async Task Post_EmptyInputWithTraceparent_Returns400ProblemFormatWith21028AndIncomingTraceId()
    {
        const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        factory.Logs.Clear();
        using var request = new HttpRequestMessage(HttpMethod.Post, ImportContent.RegisterUri) { Content = ImportContent.FormUrlEncoded("note=x") };
        request.Headers.Add("traceparent", $"00-{TraceId}-00f067aa0ba902b7-01");

        using var response = await client.SendAsync(request, CancellationToken);

        var problem = await ShouldBeValidationProblemAsync(response);
        problem.FieldCodes().Should().Equal([(string.Empty, 21028)]);
        problem.GetProperty("errors").GetProperty(string.Empty)[0].GetProperty("message").GetString().Should().Be("등록할 입력이 비어 있습니다.");
        problem.GetProperty("traceId").GetString().Should().Be(TraceId, "traceId는 들어온 traceparent의 trace-id를 잇는다");
        factory.Logs.RequestCompletions.Should().ContainSingle().Which.TraceId.ToString().Should().Be(TraceId, "요청 로그도 같은 추적 ID");
    }

    private static byte[] JsonRow(string name, string email) =>
        JsonSerializer.SerializeToUtf8Bytes(new[] { new Dictionary<string, string> { ["name"] = name, ["email"] = email, ["tel"] = "010-0000-0000", ["joined"] = "2000-01-01" } });

    private static HttpContent RequestLevelContent(string request)
    {
        var csv = Encoding.UTF8.GetBytes(ExampleFiles.OriginalCsvRow);
        switch (request)
        {
            case "form-urlencoded-without-data-key":
                return ImportContent.FormUrlEncoded(Uri.EscapeDataString(ExampleFiles.OriginalCsvRow));
            case "multipart-file-and-data":
                var both = ImportContent.MultipartFile(csv, "employees.csv");
                both.Add(new ByteArrayContent(csv), "data");
                return both;
            case "multipart-neither-file-nor-data":
                return new MultipartFormDataContent { { new ByteArrayContent(csv), "note" } };
            case "multipart-file-field-without-filename":
                return new MultipartFormDataContent { { new ByteArrayContent(csv), "file" } };
            case "multipart-data-field-with-filename":
                return new MultipartFormDataContent { { new ByteArrayContent(csv), "data", "employees.csv" } };
            case "raw-empty-body":
                return ImportContent.Raw([], "text/csv");
            case "raw-json-zero-rows":
                return ImportContent.Raw("[]"u8.ToArray(), "application/json");
            case "raw-csv-nbsp-only-lines":
                return ImportContent.Raw(Encoding.UTF8.GetBytes(" \n  \n"), "text/csv");
            case "raw-csv-1001-rows":
                return ImportContent.Raw(EmployeeImportData.Csv(1001), "text/csv");
            case "multipart-file-json-1001-rows":
                return ImportContent.MultipartFile(EmployeeImportData.Json(1001), "employees.json");
            default:
                return ImportContent.Raw(Encoding.UTF8.GetBytes("""[{"name":"김이름","email":"kim@gmail.com",""" + "\n"), "application/json");
        }
    }

    private static HttpContent MalformedTransportContent(string request)
    {
        var csv = Encoding.UTF8.GetBytes(ExampleFiles.OriginalCsvRow);
        switch (request)
        {
            case "multipart-data-twice":
                return new MultipartFormDataContent { { new ByteArrayContent(csv), "data" }, { new ByteArrayContent(csv), "data" } };
            case "multipart-file-twice":
                return new MultipartFormDataContent { { new ByteArrayContent(csv), "file", "a.csv" }, { new ByteArrayContent(csv), "file", "b.csv" } };
            case "form-urlencoded-data-twice":
                return ImportContent.FormUrlEncoded("data=a&data=b");
            case "multipart-section-with-17-headers":
                return RawMultipart(string.Concat(Enumerable.Range(1, 16).Select(index => $"X-Test-{index}: v\r\n")));
            default:
                return RawMultipart($"X-Long: {new string('a', 17 * 1024)}\r\n");
        }
    }

    private static ByteArrayContent RawMultipart(string extraHeaders)
    {
        var body = $"--b\r\nContent-Disposition: form-data; name=\"data\"\r\n{extraHeaders}\r\n{ExampleFiles.OriginalCsvRow}\r\n--b--\r\n";
        return ImportContent.Raw(Encoding.UTF8.GetBytes(body), "multipart/form-data; boundary=b");
    }

    private static int UuidVersion(Guid id) => Convert.ToInt32(id.ToString("N")[12].ToString(), 16);

    private static async Task<JsonElement> ShouldBeValidationProblemAsync(HttpResponseMessage response) =>
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, 1001, ValidationFailed, ImportContent.RegisterPath, CancellationToken);

    private static async Task<IReadOnlyList<Guid>> ReadCreatedIdsAsync(HttpResponseMessage response, int expectedCount)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        body.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal("count", "ids");
        body.RootElement.GetProperty("count").GetInt32().Should().Be(expectedCount);
        var ids = body.RootElement.GetProperty("ids").EnumerateArray().Select(id => id.GetGuid()).ToList();
        ids.Should().HaveCount(expectedCount);
        return ids;
    }

    // 저장된 행의 열 값을 "|"로 이은 문자열(id 순서)입니다.
    private async Task<IReadOnlyList<string>> ReadStoredAsync(string columns)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await using var command = new NpgsqlCommand($"SELECT {columns} FROM employees ORDER BY id", connection);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        var rows = new List<string>();
        while (await reader.ReadAsync(CancellationToken))
        {
            rows.Add(string.Join('|', Enumerable.Range(0, reader.FieldCount).Select(reader.GetString)));
        }

        return rows;
    }
}
