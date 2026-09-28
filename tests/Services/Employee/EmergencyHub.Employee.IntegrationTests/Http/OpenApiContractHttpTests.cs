using System.Net;
using System.Text.Json;
using EmergencyHub.Employee.IntegrationTests.Fixtures;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S03-T07 HTTP 인수(FR-07 "Swashbuckle(OpenAPI)"): 실제 호스트가 내보내는 swagger.json을 wiki/05-api/employee-api.md와 1회 대조한다.
// ProgramTests(단위)는 201(POST) · 404(GET) 키만 본다(S03-T04 tester 인계). 여기서는 400 · 409 응답 키 · 콘텐츠 형식과 employeeStatus 정수 enum 스키마를 본다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-07")]
public sealed class OpenApiContractHttpTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string SwaggerPath = "/swagger/v1/swagger.json";

    // ---- 성공 ----

    [Fact]
    public async Task SwaggerJson_Development_DeclaresDocumentedResponseKeysWithProblemJsonForFailures()
    {
        using var document = await GetSwaggerAsync();
        var paths = document.RootElement.GetProperty("paths");

        var post = paths.GetProperty("/api/v1/employees").GetProperty("post").GetProperty("responses");
        ResponseKeys(post).Should().Equal("201", "400", "409");
        FailureContentTypes(post, "400", "409").Should().AllBe(HttpProblem.ContentType);

        var get = paths.GetProperty("/api/v1/employees/{id}").GetProperty("get");
        ResponseKeys(get.GetProperty("responses")).Should().Equal("200", "400", "404");
        FailureContentTypes(get.GetProperty("responses"), "400", "404").Should().AllBe(HttpProblem.ContentType);
        var id = get.GetProperty("parameters")[0];
        id.GetProperty("name").GetString().Should().Be("id");
        id.GetProperty("schema").GetProperty("format").GetString().Should().Be("uuid");
    }

    [Fact]
    public async Task SwaggerJson_EmployeeStatus_IsIntegerEnumWithDocumentedValuesAndNames()
    {
        using var document = await GetSwaggerAsync();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        var status = schemas.GetProperty("EmployeeStatus");
        status.GetProperty("type").GetString().Should().Be("integer", "코드값은 정수(ADR-0008), 문자열 enum 금지");
        status.GetProperty("enum").EnumerateArray().Select(value => value.GetInt32()).Should().Equal(0, 1, 2);
        status.GetProperty("description").GetString().Should().Be("0 = Unknown, 1 = Active, 2 = Inactive", "employee-api.md 코드값 표와 같다");

        foreach (var owner in new[] { "RegisterEmployeeRequest", "EmployeeResponse" })
        {
            schemas.GetProperty(owner).GetProperty("properties").GetProperty("employeeStatus").GetProperty("$ref").GetString()
                .Should().Be("#/components/schemas/EmployeeStatus");
        }

        var problem = schemas.GetProperty("ProblemDetails");
        problem.GetProperty("properties").GetProperty("code").GetProperty("type").GetString().Should().Be("integer");
        problem.GetProperty("required").EnumerateArray().Select(value => value.GetString()).Should().BeEquivalentTo("code", "traceId");
    }

    private static string[] ResponseKeys(JsonElement responses) => [.. responses.EnumerateObject().Select(response => response.Name)];

    private static string[] FailureContentTypes(JsonElement responses, params string[] keys) =>
        [.. keys.SelectMany(key => responses.GetProperty(key).GetProperty("content").EnumerateObject().Select(content => content.Name))];

    private async Task<JsonDocument> GetSwaggerAsync()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(SwaggerPath, UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
    }
}
