using System.Text.Json;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.Employee.Api.OpenApi;
using EmergencyHub.Employee.Api.UnitTests.TestDoubles;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmergencyHub.Employee.Api.UnitTests.OpenApi;

// S06-T05 완료 조건 ⑤(PRD-002 FR-05, ADR-0019): Swagger UI에서 file · data · raw를 시험할 수 있도록 요청 본문을 전송 형식 4종으로 적는다.
// 실제 문서(Development, /swagger/v1/swagger.json)를 읽어 확인하고, 필터 단위로 대상 밖 액션은 건드리지 않는지 본다.
[Trait("FR", "PRD-002/FR-05")]
public sealed class EmployeeImportOperationFilterTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // ---- 성공: 실제 문서 ----

    [Fact]
    public async Task SwaggerDocument_RegisterOperation_HasFourRequestBodyTypesAndNoPayloadParameter()
    {
        var post = (await ReadDocumentAsync()).GetProperty("paths").GetProperty("/api/employee").GetProperty("post");

        if (post.TryGetProperty("parameters", out var parameters))
        {
            parameters.GetArrayLength().Should().Be(0, "전용 바인더 매개변수(payload)는 쿼리 매개변수로 나오지 않아야 한다");
        }

        var body = post.GetProperty("requestBody");
        body.GetProperty("required").GetBoolean().Should().BeTrue();
        var content = body.GetProperty("content");
        content.EnumerateObject().Select(media => media.Name).Should().Equal(
            "multipart/form-data", "application/x-www-form-urlencoded", "text/csv", "application/json");

        var multipart = content.GetProperty("multipart/form-data").GetProperty("schema").GetProperty("properties");
        multipart.GetProperty("file").GetProperty("type").GetString().Should().Be("string");
        multipart.GetProperty("file").GetProperty("format").GetString().Should().Be("binary");
        multipart.GetProperty("data").GetProperty("type").GetString().Should().Be("string");
        content.GetProperty("application/x-www-form-urlencoded").GetProperty("schema").GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).Should().Equal("data");
        content.GetProperty("text/csv").GetProperty("schema").GetProperty("type").GetString().Should().Be("string");
        content.GetProperty("application/json").GetProperty("schema").GetProperty("type").GetString().Should().Be("array");
        content.GetProperty("application/json").GetProperty("example").ValueKind.Should().Be(JsonValueKind.Array, "Swagger UI가 JSON 문자열이 아니라 배열을 보내야 한다");
    }

    [Fact]
    public async Task SwaggerDocument_RegisterOperation_DeclaresSuccessAndFailureResponses()
    {
        var responses = (await ReadDocumentAsync()).GetProperty("paths").GetProperty("/api/employee").GetProperty("post").GetProperty("responses");

        responses.EnumerateObject().Select(response => response.Name).Should().BeEquivalentTo("201", "400", "409", "413", "415");
        responses.GetProperty("413").GetProperty("content").EnumerateObject().Select(media => media.Name).Should().Equal("application/problem+json");
    }

    // ---- 실패: 대상 밖 액션은 그대로 ----

    [Fact]
    public void Apply_OperationWithoutPayloadParameter_LeavesOperationUnchanged()
    {
        var operation = new OpenApiOperation { Parameters = [new OpenApiParameter { Name = "payload", In = ParameterLocation.Query }] };
        var description = new ApiDescription { ActionDescriptor = new ActionDescriptor() };
        description.ParameterDescriptions.Add(new ApiParameterDescription { Name = "payload", Type = typeof(string) });

        new EmployeeImportOperationFilter().Apply(operation, Context(description));

        operation.RequestBody.Should().BeNull();
        operation.Parameters.Should().ContainSingle();
    }

    [Fact]
    public void Apply_NullArguments_Throw()
    {
        var filter = new EmployeeImportOperationFilter();
        var description = new ApiDescription { ActionDescriptor = new ActionDescriptor() };

        filter.Invoking(target => target.Apply(null!, Context(description))).Should().Throw<ArgumentNullException>();
        filter.Invoking(target => target.Apply(new OpenApiOperation(), null!)).Should().Throw<ArgumentNullException>();
    }

    // ---- 엣지: 매개변수 목록이 없어도 요청 본문을 둔다 ----

    [Fact]
    public void Apply_PayloadParameterWithoutGeneratedParameters_StillSetsRequestBody()
    {
        var operation = new OpenApiOperation();
        var description = new ApiDescription { ActionDescriptor = new ActionDescriptor() };
        description.ParameterDescriptions.Add(new ApiParameterDescription { Name = "payload", Type = typeof(Api.Employees.Import.EmployeeImportPayload) });

        new EmployeeImportOperationFilter().Apply(operation, Context(description));

        operation.RequestBody!.Content!.Keys.Should().HaveCount(4);
    }

    private static OperationFilterContext Context(ApiDescription description) =>
        new(description, Substitute.For<ISchemaGenerator>(), new SchemaRepository(), new OpenApiDocument(), typeof(EmployeeImportOperationFilterTests).GetMethod(nameof(Context), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!);

    private static async Task<JsonElement> ReadDocumentAsync()
    {
        await using var host = await EmployeeApiTestHost.StartAsync(Substitute.For<ISender>(), "Development", CancellationToken);
        var json = await host.Client.GetStringAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative), CancellationToken);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
