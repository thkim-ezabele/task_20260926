using System.Globalization;
using System.Text;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Domain.Employees.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployees;

// S06-T04 일괄 등록 Handler(PRD-002 FR-06, ADR-0026 6 · 7 · 9절): ① 파싱 → ② 행 검증(파서 행 오류 + Value Object Create 결과) →
// ③ 요청 안 이메일 중복 → ④ DB 사전 조회 → ⑤ Aggregate 생성 · AddRange. 앞 단계가 실패하면 멈추고 뒤 단계 대역(Repository · IIdGenerator)을 부르지 않는다.
// ① 뒤 행 0개(오류도 0개)는 21028(경로 "", S07-T05). ①~③ 실패는 ValidationError(1001), ④ 실패는 ConflictError(23001 + Rows[n].Email). 목록은 각각 최대 100개 + 잘림 항목(21030 · 23002, 경로 "").
// Handler는 저장하지 않으므로 SaveChanges · CommitAsync를 검증하지 않는다(ADR-0014).
[Trait("FR", "PRD-002/FR-06")]
[Trait("FR", "PRD-002/FR-10")]
[Trait("NFR", "PRD-002/NFR-04")]
public sealed class RegisterEmployeesCommandHandlerTests
{
    private const string ValidTel = "010-1234-5678";
    private const string ValidJoined = "2020-01-02";

    private static readonly string[] LogStateKeys = ["EmployeeId", "{OriginalFormat}"];

    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IIdGenerator _idGenerator = Substitute.For<IIdGenerator>();
    private readonly FakeLogger<RegisterEmployeesCommandHandler> _logger = new();
    private int _nextId;

    public RegisterEmployeesCommandHandlerTests()
    {
        _idGenerator.NewId().Returns(_ => IdOf(++_nextId));
        _repository.ListExistingNormalizedEmailsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    // ---- 성공 ----

    [Fact]
    public async Task Handle_AllRowsValid_AddsEmployeesInInputOrderAndReturnsCountAndIds()
    {
        List<EmployeeAggregate>? added = null;
        _repository.AddRange(Arg.Do<IEnumerable<EmployeeAggregate>>(employees => added = [.. employees]));

        var result = await Handle(Csv(
            "홍길동,Hong@Example.com,010-1111-2222,2020-01-02",
            "김철수,kim@example.com,01033334444,1999-12-31",
            "이영희,lee@example.com,02-123-4567,2024-02-29"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(3);
        result.Value.Ids.Should().Equal(IdOf(1), IdOf(2), IdOf(3));
        _repository.Received(1).AddRange(Arg.Any<IEnumerable<EmployeeAggregate>>());
        added.Should().NotBeNull();
        added!.Select(employee => employee.Id.Value).Should().Equal(result.Value.Ids);
        added.Select(employee => employee.Name.Value).Should().Equal("홍길동", "김철수", "이영희");
        added.Select(employee => employee.Email.Value).Should().Equal("Hong@Example.com", "kim@example.com", "lee@example.com");
        added.Select(employee => employee.NormalizedEmail).Should().Equal("hong@example.com", "kim@example.com", "lee@example.com");
        added.Select(employee => employee.PhoneNumber.Value).Should().Equal("010-1111-2222", "01033334444", "02-123-4567");
        added.Select(employee => employee.JoinedOn.Value).Should().Equal(new DateOnly(2020, 1, 2), new DateOnly(1999, 12, 31), new DateOnly(2024, 2, 29));
    }

    [Fact]
    public async Task Handle_AllRowsValid_RegistersActiveEmployeesWithDomainEvents()
    {
        List<EmployeeAggregate>? added = null;
        _repository.AddRange(Arg.Do<IEnumerable<EmployeeAggregate>>(employees => added = [.. employees]));

        await Handle(Csv(Row("a@example.com"), Row("b@example.com")));

        added.Should().HaveCount(2);
        added!.Should().OnlyContain(employee => employee.EmployeeStatus == EmployeeStatus.Active);
        added.Select(employee => employee.DomainEvents.Should().ContainSingle().Subject)
            .Should().Equal(new EmployeeRegisteredDomainEvent(new EmployeeId(IdOf(1))), new EmployeeRegisteredDomainEvent(new EmployeeId(IdOf(2))));
    }

    [Fact]
    public async Task Handle_AllRowsValid_QueriesDatabaseOnceWithNormalizedEmailsInInputOrder()
    {
        IReadOnlyCollection<string>? queried = null;
        _repository.ListExistingNormalizedEmailsAsync(
                Arg.Do<IReadOnlyCollection<string>>(emails => queried = [.. emails]), Arg.Any<CancellationToken>())
            .Returns([]);

        await Handle(Csv(Row("  B@Example.COM "), Row("a@example.com")));

        await _repository.Received(1).ListExistingNormalizedEmailsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
        queried.Should().Equal("b@example.com", "a@example.com");
    }

    [Fact]
    public async Task Handle_JsonInput_UsesJsonParser()
    {
        var result = await Handle(Json("""{"name":"홍길동","email":"hong@example.com","tel":"010-1234-5678","joined":"2020-01-02"}"""));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new RegisterEmployeesResponse(1, [IdOf(1)]));
    }

    [Fact]
    public async Task Handle_AllRowsValid_LogsEmployeeRegisteredPerEmployeeWithIdOnly()
    {
        await Handle(Csv(Row("a@example.com"), Row("b@example.com")));

        var records = _logger.Collector.GetSnapshot();
        records.Should().HaveCount(2);
        records.Should().OnlyContain(record => record.Level == LogLevel.Information && record.Id.Id == 20001);
        records.Select(record => record.GetStructuredStateValue("EmployeeId")).Should().Equal(IdOf(1).ToString(), IdOf(2).ToString());
        records.Should().OnlyContain(record =>
            record.StructuredState!.Select(pair => pair.Key).SequenceEqual(LogStateKeys));
        records.Should().OnlyContain(record => !record.Message.Contains('@') && !record.Message.Contains("홍길동"));
    }

    [Fact]
    public async Task Handle_Always_PassesCancellationTokenToRepository()
    {
        using var cancellation = new CancellationTokenSource();

        await CreateSut().Handle(Csv(Row("a@example.com")), cancellation.Token);

        await _repository.Received(1).ListExistingNormalizedEmailsAsync(Arg.Any<IReadOnlyCollection<string>>(), cancellation.Token);
    }

    // ---- 실패: ① 파싱(요청 전체 오류, 경로 "") ----

    [Fact]
    public async Task Handle_InvalidUtf8_ReturnsParserErrorAndCallsNothingElse()
    {
        var result = await Handle(new RegisterEmployeesCommand(EmployeeImportFormat.Csv, EmployeeImportSources.Body, new byte[] { 0x41, 0xFF }));

        result.Error.Should().Be(Validation((string.Empty, EmployeeErrors.ImportInvalidUtf8)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_JsonSyntaxError_ReturnsParserErrorAndCallsNothingElse()
    {
        var result = await Handle(Json("[{\"name\":\"a\"},]"));

        result.Error.Should().Be(Validation((string.Empty, EmployeeErrors.JsonSyntaxInvalid)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_TooManyRows_ReturnsParserErrorAndCallsNothingElse()
    {
        var rows = Enumerable.Range(1, 1001).Select(number => Row(Email(number))).ToArray();

        var result = await Handle(Csv(rows));

        result.Error.Should().Be(Validation((string.Empty, EmployeeErrors.ImportTooManyRows)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    // ---- 실패: ② 행 검증 ----

    [Fact]
    public async Task Handle_CsvRowError_ReturnsRowPathAndCallsNothingElse()
    {
        var result = await Handle(Csv(Row("a@example.com"), "홍길동,b@example.com,010-1234-5678"));

        result.Error.Should().Be(Validation(("Rows[2]", EmployeeErrors.CsvColumnCountMismatch)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_JsonItemErrors_UseFieldPathWhenParserNamesField()
    {
        var result = await Handle(Json("""
            [
              {"name":"홍길동","email":"a@example.com","tel":"010-1234-5678","joined":20200102},
              null,
              {"name":"홍길동","NAME":"김철수","email":"c@example.com","tel":"010-1234-5678","joined":"2020-01-02"}
            ]
            """));

        result.Error.Should().Be(Validation(
            ("Rows[1].Joined", EmployeeErrors.JsonValueNotString),
            ("Rows[2]", EmployeeErrors.JsonItemNotObject),
            ("Rows[3].Name", EmployeeErrors.JsonDuplicateProperty)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_InvalidFields_ReportsEachFieldInFieldOrderWithRowPaths()
    {
        var result = await Handle(Csv(
            Row("a@example.com"),
            "홍길동,not-an-email,010-1234-5678,2020-01-02",
            ",b@@example.com,+82-10-1234-5678,2000-2-3"));

        result.Error.Should().Be(Validation(
            ("Rows[2].Email", EmployeeErrors.EmailInvalid),
            ("Rows[3].Name", EmployeeErrors.NameRequired),
            ("Rows[3].Email", EmployeeErrors.EmailInvalid),
            ("Rows[3].Tel", EmployeeErrors.PhoneNumberInvalidCharacter),
            ("Rows[3].Joined", EmployeeErrors.JoinedOnInvalidFormat)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_JsonMissingProperties_ReturnsRequiredCodes()
    {
        var result = await Handle(Json("""{"name":"홍길동"}"""));

        result.Error.Should().Be(Validation(
            ("Rows[1].Email", EmployeeErrors.EmailRequired),
            ("Rows[1].Tel", EmployeeErrors.PhoneNumberRequired),
            ("Rows[1].Joined", EmployeeErrors.JoinedOnRequired)));
    }

    [Fact]
    public async Task Handle_ParserRowErrorsAndFieldErrors_AreMergedInRowOrder()
    {
        // 행 번호는 CSV 물리 줄 번호다(빈 줄 2를 센다). 파서 행 오류(4행)와 필드 오류(1 · 5행)가 행 순서로 한 목록에 섞인다.
        var result = await Handle(Csv(
            "홍길동,a@example.com,010-1234-5678,1899-12-31",
            string.Empty,
            Row("b@example.com"),
            "a,b",
            "홍길동,c@example.com,1234,2020-01-02"));

        result.Error.Should().Be(Validation(
            ("Rows[1].Joined", EmployeeErrors.JoinedOnTooEarly),
            ("Rows[4]", EmployeeErrors.CsvColumnCountMismatch),
            ("Rows[5].Tel", EmployeeErrors.PhoneNumberDigitCountOutOfRange)));
    }

    [Fact]
    public async Task Handle_ControlCharacterInEmail_ReturnsEmailInvalid()
    {
        // CSV 파서는 NUL · 제어 문자를 보존하고 Email Value Object가 21004로 거부한다(BL-129).
        var result = await Handle(Csv("홍길동,hong\0@example.com,010-1234-5678,2020-01-02"));

        result.Error.Should().Be(Validation(("Rows[1].Email", EmployeeErrors.EmailInvalid)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_RowErrorsWithDuplicateEmails_StopsBeforeDuplicateCheck()
    {
        var result = await Handle(Csv(Row("a@example.com"), Row("a@example.com"), "홍길동,b@example.com,x,2020-01-02"));

        result.Error.Should().Be(Validation(("Rows[3].Tel", EmployeeErrors.PhoneNumberInvalidCharacter)));
    }

    // ---- 실패: ③ 요청 안 중복 ----

    [Fact]
    public async Task Handle_DuplicateEmailsDifferingInCase_MarksBothRowsAndCallsNothingElse()
    {
        var result = await Handle(Csv(Row("Hong@Example.com"), Row("kim@example.com"), Row("hong@example.COM")));

        result.Error.Should().Be(Validation(
            ("Rows[1].Email", EmployeeErrors.DuplicateEmailInRequest),
            ("Rows[3].Email", EmployeeErrors.DuplicateEmailInRequest)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_ThreeOrMoreDuplicatesInSeveralGroups_MarksEveryRelatedRowInRowOrder()
    {
        var result = await Handle(Csv(
            Row("a@example.com"),
            Row("a@example.com"),
            Row("b@example.com"),
            Row("A@example.com"),
            Row("B@EXAMPLE.COM"),
            Row("c@example.com")));

        result.Error.Should().Be(Validation(
            ("Rows[1].Email", EmployeeErrors.DuplicateEmailInRequest),
            ("Rows[2].Email", EmployeeErrors.DuplicateEmailInRequest),
            ("Rows[3].Email", EmployeeErrors.DuplicateEmailInRequest),
            ("Rows[4].Email", EmployeeErrors.DuplicateEmailInRequest),
            ("Rows[5].Email", EmployeeErrors.DuplicateEmailInRequest)));
    }

    [Fact]
    public async Task Handle_EmailsDifferingOnlyByTurkishDottedCapitalI_AreNotDuplicates()
    {
        // 중복 판정은 NormalizedEmail(ToLowerInvariant) 서수 비교다. U+0130은 바뀌지 않으므로 i와 다른 값이다(ADR-0027).
        var result = await Handle(Csv(Row("İ@example.com"), Row("i@example.com")));

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2);
    }

    // ---- 실패: ④ DB 사전 조회 ----

    [Fact]
    public async Task Handle_EmailAlreadyStored_ReturnsConflictWithRowNumberAndAddsNothing()
    {
        ExistingEmailsAre("b@example.com");

        var result = await Handle(Csv(Row("a@example.com"), Row("B@Example.com"), Row("c@example.com")));

        result.Error.Should().Be(Conflict((2, EmployeeErrors.DuplicateEmail)));
        result.Error.Code.Should().Be(23001);
        _idGenerator.DidNotReceive().NewId();
        _repository.DidNotReceive().AddRange(Arg.Any<IEnumerable<EmployeeAggregate>>());
        _logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_StoredEmailsReturnedInAnyOrder_AreReportedInInputOrder()
    {
        // Repository 결과 순서는 정하지 않는다. NormalizedEmail → 행 번호로 짝지어 입력 순서로 보고한다.
        ExistingEmailsAre("e@example.com", "a@example.com", "c@example.com");

        var result = await Handle(Csv(
            Row("a@example.com"), Row("b@example.com"), Row("c@example.com"), Row("d@example.com"), Row("e@example.com")));

        result.Error.Should().Be(Conflict((1, EmployeeErrors.DuplicateEmail), (3, EmployeeErrors.DuplicateEmail), (5, EmployeeErrors.DuplicateEmail)));
    }

    [Fact]
    public async Task Handle_RepositoryReturnsRepeatedOrUnknownValues_ReportsEachMatchingRowOnce()
    {
        ExistingEmailsAre("b@example.com", "b@example.com", "zzz@example.com");

        var result = await Handle(Csv(Row("a@example.com"), Row("b@example.com")));

        result.Error.Should().Be(Conflict((2, EmployeeErrors.DuplicateEmail)));
    }

    [Fact]
    public async Task Handle_RepositoryReturnsOnlyUnknownValues_Succeeds()
    {
        // 넘기지 않은 값은 짝지을 행이 없으므로 충돌로 보지 않는다(Repository 계약 밖 값).
        ExistingEmailsAre("zzz@example.com");

        var result = await Handle(Csv(Row("a@example.com")));

        result.IsSuccess.Should().BeTrue();
    }

    // ---- 엣지: 목록 최대 100개 + 잘림 항목 ----

    [Fact]
    public async Task Handle_ExactlyHundredRowErrors_ReportsAllWithoutTruncationItem()
    {
        var result = await Handle(Csv(Enumerable.Range(1, 100).Select(_ => "a,b").ToArray()));

        var errors = ((ValidationError)result.Error).Errors;
        errors.Should().HaveCount(100);
        errors.Should().OnlyContain(error => error.Code == EmployeeErrors.CsvColumnCountMismatch.Code);
        errors[^1].PropertyName.Should().Be("Rows[100]");
    }

    [Fact]
    public async Task Handle_MoreThanHundredRowErrors_KeepsFirstHundredAndAddsTruncationItem()
    {
        // 행 오류(파서)와 필드 오류를 합친 목록의 앞 100개 + 21030(경로 "")이다.
        var rows = Enumerable.Range(1, 60).Select(_ => "a,b")
            .Concat(Enumerable.Range(1, 60).Select(number => Row("bad" + number.ToString(CultureInfo.InvariantCulture))))
            .ToArray();

        var result = await Handle(Csv(rows));

        var errors = ((ValidationError)result.Error).Errors;
        errors.Should().HaveCount(101);
        errors.Take(60).Should().OnlyContain(error => error.Code == EmployeeErrors.CsvColumnCountMismatch.Code);
        errors.Skip(60).Take(40).Should().OnlyContain(error => error.Code == EmployeeErrors.EmailInvalid.Code);
        errors[99].PropertyName.Should().Be("Rows[100].Email");
        errors[^1].Should().Be(FieldError.Create(string.Empty, EmployeeErrors.RowErrorsTruncated));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_MoreThanHundredDuplicateRows_KeepsFirstHundredAndAddsTruncationItem()
    {
        var result = await Handle(Csv(Enumerable.Range(1, 150).Select(_ => Row("same@example.com")).ToArray()));

        var errors = ((ValidationError)result.Error).Errors;
        errors.Should().HaveCount(101);
        errors.Take(100).Should().Equal(Enumerable.Range(1, 100).Select(number => FieldError.Create(RowPath(number, "Email"), EmployeeErrors.DuplicateEmailInRequest)));
        errors[^1].Should().Be(FieldError.Create(string.Empty, EmployeeErrors.RowErrorsTruncated));
    }

    [Fact]
    public async Task Handle_ExactlyHundredConflicts_ReportsAllWithoutTruncationItem()
    {
        var emails = Enumerable.Range(1, 100).Select(Email).ToArray();
        ExistingEmailsAre(emails);

        var result = await Handle(Csv(emails.Select(Row).ToArray()));

        var details = ((ConflictError)result.Error).Details;
        details.Should().HaveCount(100);
        details[^1].Should().Be(ConflictDetail.Create("Rows[100].Email", EmployeeErrors.DuplicateEmail));
    }

    [Fact]
    public async Task Handle_MoreThanHundredConflicts_KeepsFirstHundredInInputOrderAndAddsTruncationItem()
    {
        var emails = Enumerable.Range(1, 101).Select(Email).ToArray();
        ExistingEmailsAre([.. emails.Reverse()]);

        var result = await Handle(Csv(emails.Select(Row).ToArray()));

        var conflict = (ConflictError)result.Error;
        conflict.Code.Should().Be(EmployeeErrors.DuplicateEmail.Code);
        conflict.Details.Should().HaveCount(101);
        conflict.Details.Take(100).Should().Equal(
            Enumerable.Range(1, 100).Select(number => ConflictDetail.Create(RowPath(number, "Email"), EmployeeErrors.DuplicateEmail)));
        conflict.Details[^1].Should().Be(ConflictDetail.Create(string.Empty, EmployeeErrors.RowConflictsTruncated));
    }

    // ---- 실패: 행 0개(BL-137 결정 A, S07-T05) ----
    // Validator 공백 집합(BOM 뒤 0x20 · 0x09 · 0x0D · 0x0A) 밖이라 Validator를 지나지만 파서가 행 0개로 읽는 입력은
    // Handler가 파싱 직후 21028(경로 "")로 거부하고 DB 조회 · AddRange를 하지 않는다.

    [Theory]
    [InlineData("[]")]
    [InlineData("[\n]")]
    [InlineData(" [ ]\r\n")]
    public async Task Handle_JsonWithZeroRows_ReturnsImportInputEmptyAndCallsNothingElse(string json)
    {
        var result = await Handle(Json(json));

        result.Error.Should().Be(Validation((string.Empty, EmployeeErrors.ImportInputEmpty)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData(" \n\n  ")]
    [InlineData("　\r\n \t")]
    public async Task Handle_CsvWithOnlyBlankLinesOutsideValidatorWhitespace_ReturnsImportInputEmptyAndCallsNothingElse(string csv)
    {
        var result = await Handle(Csv(csv));

        result.Error.Should().Be(Validation((string.Empty, EmployeeErrors.ImportInputEmpty)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    // ---- 엣지: 행 0개 경계 · 형식 ----

    [Fact]
    public async Task Handle_ZeroReadRowsButParserRowError_ReportsRowErrorNotImportInputEmpty()
    {
        // 읽은 행은 0개지만 행 오류가 있으면 빈 입력이 아니다. 21028이 아니라 행 오류를 보고한다.
        var result = await Handle(Json("[null]"));

        result.Error.Should().Be(Validation(("Rows[1]", EmployeeErrors.JsonItemNotObject)));
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_OneRowAfterNbspBlankLines_RegistersThatRow()
    {
        // 행이 1개 이상이면 지금 동작 그대로다. 빈 줄은 줄 번호에만 센다.
        var result = await Handle(Csv(" ", Row("a@example.com"), " "));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new RegisterEmployeesResponse(1, [IdOf(1)]));
        _repository.Received(1).AddRange(Arg.Any<IEnumerable<EmployeeAggregate>>());
    }

    [Fact]
    public async Task Handle_UndefinedFormat_ThrowsBecauseValidatorMustRejectItFirst()
    {
        var command = new RegisterEmployeesCommand(EmployeeImportFormat.Unknown, EmployeeImportSources.Body, Encoding.UTF8.GetBytes(Row("a@example.com")));

        var act = () => Handle(command);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().NotContain("@");
        ShouldNotReachDatabaseOrIdGenerator();
    }

    [Fact]
    public async Task Handle_ErrorMessages_AreFixedTextsWithoutInputValues()
    {
        // NFR-04: 오류 메시지에 입력 값(이메일 · 이름)이 들어가지 않는다.
        var result = await Handle(Csv("비밀이름,secret@@example.com,010-1234-5678,2020-01-02", Row("dup@example.com"), Row("dup@example.com")));

        var errors = ((ValidationError)result.Error).Errors;
        errors.Should().OnlyContain(error => !error.Message.Contains("secret") && !error.Message.Contains("비밀이름"));
    }

    private static Guid IdOf(int number) => new(number, 0, 0x7000, [0x80, 0, 0, 0, 0, 0, 0, 1]);

    private static string Email(int number) => "user" + number.ToString(CultureInfo.InvariantCulture) + "@example.com";

    private static string Row(string email) => "홍길동," + email + "," + ValidTel + "," + ValidJoined;

    private static string RowPath(int number, string field) =>
        string.Create(CultureInfo.InvariantCulture, $"Rows[{number}].{field}");

    private static RegisterEmployeesCommand Csv(params string[] lines) =>
        new(EmployeeImportFormat.Csv, EmployeeImportSources.Body, Encoding.UTF8.GetBytes(string.Join('\n', lines)));

    private static RegisterEmployeesCommand Json(string json) =>
        new(EmployeeImportFormat.Json, EmployeeImportSources.Body, Encoding.UTF8.GetBytes(json));

    private static ValidationError Validation(params (string Path, Error Error)[] errors) =>
        ValidationError.Create(errors.Select(error => FieldError.Create(error.Path, error.Error)));

    private static ConflictError Conflict(params (int Row, Error Error)[] rows) =>
        ConflictError.Create(
            EmployeeErrors.DuplicateEmail,
            rows.Select(row => ConflictDetail.Create(RowPath(row.Row, "Email"), row.Error)));

    private void ExistingEmailsAre(params string[] normalizedEmails) =>
        _repository.ListExistingNormalizedEmailsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([.. normalizedEmails]);

    private void ShouldNotReachDatabaseOrIdGenerator()
    {
        _repository.ReceivedCalls().Should().BeEmpty();
        _idGenerator.DidNotReceive().NewId();
        _logger.Collector.Count.Should().Be(0);
    }

    private Task<BuildingBlocks.Domain.Results.Result<RegisterEmployeesResponse>> Handle(RegisterEmployeesCommand command) =>
        CreateSut().Handle(command, TestContext.Current.CancellationToken);

    private RegisterEmployeesCommandHandler CreateSut() => new(_repository, _idGenerator, _logger);
}
