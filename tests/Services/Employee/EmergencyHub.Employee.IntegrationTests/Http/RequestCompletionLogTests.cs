using System.Net;
using System.Net.Http.Json;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.ServiceDefaults;
using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S03-T07 결함 수정(ADR-0020 "요청 로그", evidence/S03-T05 '발견 사항' 1): 요청 완료 로그가 정적 Serilog.Log(무음)로 가서 어디에도 남지 않았다.
// 실제 Api 파이프라인에서 비헬스 요청 1건당 완료 로그 1줄, 헬스 요청은 0줄(Verbose → 최소 수준에서 걸러짐)인지 수집 싱크로 확인한다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-03")]
[Trait("FR", "PRD-001/FR-09")]
public sealed class RequestCompletionLogTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string EmployeesPath = "/api/v1/employees";

    // ---- 성공 ----

    [Fact]
    public async Task PostEmployee_Created_WritesExactlyOneCompletionLogAtInformation()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            EmployeesPath,
            new { displayName = "Request Log", email = "request-log@example.com", employeeStatus = 1 },
            CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var completion = factory.Logs.RequestCompletions.Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Information);
        completion.Properties["RequestMethod"].ToString().Should().Be("\"POST\"");
        completion.Properties["RequestPath"].ToString().Should().Be($"\"{EmployeesPath}\"");
        completion.Properties["StatusCode"].ToString().Should().Be("201");
        completion.Properties["ServiceName"].ToString().Should().Be("\"employee\"");
    }

    // ---- 실패 ----

    [Fact]
    public async Task PostEmployee_ValidationFailure_WritesOneCompletionLogWith400()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(EmployeesPath, new { displayName = "No Status", email = "no-status@example.com" }, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var completion = factory.Logs.RequestCompletions.Should().ContainSingle().Which;
        completion.Level.Should().Be(LogEventLevel.Information, "4xx는 Error가 아니다(RequestLogLevels)");
        completion.Properties["StatusCode"].ToString().Should().Be("400");
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData(HealthEndpoints.LivePath)]
    [InlineData(HealthEndpoints.ReadyPath)]
    public async Task GetHealth_WritesNoCompletionLog(string path)
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.Logs.RequestCompletions.Should().BeEmpty("헬스 요청은 Verbose로 낮춰 Development 최소 수준(Debug)에서 걸러진다");
    }
}
