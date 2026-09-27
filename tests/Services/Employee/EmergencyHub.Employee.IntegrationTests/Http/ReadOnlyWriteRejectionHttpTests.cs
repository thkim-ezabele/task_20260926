using System.Net;
using System.Net.Http.Json;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Npgsql;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S03-T07 HTTP 인수(FR-08 "읽기 연결로 쓰기 시도 시 DB 거부", BL-082 S2 응답 수준 · BL-083 H4).
// 쓰기 DbContext에 읽기 전용 연결(default_transaction_read_only=on)을 넣으면 INSERT가 25006으로 거부되고, 변환되지 않는 DB 예외라 500 · 9001이다.
// 로그 단언은 Production 환경에서 한다(메인 세션 판단): Development는 EF Database.Command 범주가 Information이라 SQL 문(파라미터 값 없음)이 정상적으로 수집된다.
// DB 수준 25006 자체(SQL · ReadDbContext)는 EmployeeSchemaTests가 본다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-08")]
[Trait("FR", "PRD-001/FR-07")]
public sealed class ReadOnlyWriteRejectionHttpTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string EmployeesPath = "/api/v1/employees";
    private const string Email = "readonly-write@example.com";

    // 응답 본문 · 수집 로그 어디에도 나오면 안 되는 문자열: SQL, 테이블 · 제약 · 인덱스 이름, SQLSTATE, 서버 원본 메시지,
    // ExceptionHandlerMiddleware의 원본 예외 로그 문구, 요청 파라미터 값(이메일).
    private static readonly string[] ForbiddenFragments =
    [
        "INSERT INTO",
        "SELECT ",
        "RETURNING",
        "ux_employees_email",
        "ck_employees_employee_status",
        "pk_employees",
        "25006",
        "read-only transaction",
        "An unhandled exception has occurred while executing the request",
        Email,
    ];

    // ---- 실패 ----

    [Fact]
    public async Task Post_WriteDbContextOnReadOnlyConnectionInProduction_Returns500With9001AndLeaksNoSqlOrConstraint()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            Environment = "Production",
            WriteConnectionString = Database.ReadConnectionString,
        });
        using var client = factory.CreateClient();
        factory.Logs.Clear();

        using var response = await client.PostAsJsonAsync(EmployeesPath, new { displayName = "Read Only", email = Email, employeeStatus = 1 }, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.InternalServerError, 9001, "예상하지 못한 오류가 발생했습니다.", EmployeesPath, CancellationToken);
        var body = problem.GetRawText();
        body.Should().NotContainAny(ForbiddenFragments);
        body.Should().NotContain("Npgsql").And.NotContain("DbUpdateException");

        var events = factory.Logs.Events;
        var unhandled = events.Where(e => e.EventId() == 1).Should().ContainSingle().Which;
        unhandled.Level.Should().Be(LogEventLevel.Error);
        unhandled.Properties["ExceptionType"].ToString().Should().Be("\"Microsoft.EntityFrameworkCore.DbUpdateException\"");
        unhandled.Exception!.InnerException.Should().NotBeNull("형식 이름 · 스택은 내부 예외 사슬까지 남는다(메시지만 뺌)");

        var completion = factory.Logs.RequestCompletions.Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Error);
        completion.Properties["StatusCode"].ToString().Should().Be("500");

        events.Should().NotContain(e => e.SourceContext().Contains("ExceptionHandlerMiddleware", StringComparison.Ordinal), "원본 예외를 싣는 범주는 꺼져 있다");
        foreach (var text in events.Select(LogEventText.Dump))
        {
            text.Should().NotContainAny(ForbiddenFragments);
        }

        (await CountEmployeesAsync()).Should().Be(0, "25006으로 거부되어 저장된 행이 없다");
    }

    private async Task<long> CountEmployeesAsync()
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        await using var command = new NpgsqlCommand("SELECT count(*) FROM employees", connection);
        return (long)(await command.ExecuteScalarAsync(CancellationToken))!;
    }
}
