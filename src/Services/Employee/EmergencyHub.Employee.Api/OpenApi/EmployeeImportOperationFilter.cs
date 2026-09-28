using System.Text.Json.Nodes;
using EmergencyHub.Employee.Api.Controllers;
using EmergencyHub.Employee.Api.Employees.Import;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmergencyHub.Employee.Api.OpenApi;

/// <summary>
/// 일괄 등록 액션(매개변수 <see cref="EmployeeImportPayload"/>)의 OpenAPI 요청 본문을 전송 형식 4종으로 적어 Swagger UI에서 시험할 수 있게 합니다
/// (PRD-002 FR-05, ADR-0019).
/// </summary>
/// <remarks>
/// 전용 바인더 매개변수는 ApiExplorer가 본문으로 알지 못해 쿼리 매개변수로 나오므로 지우고, 요청 본문을 직접 둡니다:
/// <c>multipart/form-data</c>(파일 <c>file</c> · 텍스트 <c>data</c>), <c>application/x-www-form-urlencoded</c>(<c>data</c>),
/// <c>text/csv</c> · <c>application/json</c>(raw). 둘 이상 보내면 400(21029)인 규칙은 설명에 적습니다(스키마로 강제하지 않음).
/// </remarks>
internal sealed class EmployeeImportOperationFilter : IOperationFilter
{
    private const string CsvExample = "김이름,kim@gmail.com,010-0000-0000,2000-01-01";
    private const string JsonExample = """[{ "name": "김이름", "email": "kim@gmail.com", "tel": "010-0000-0000", "joined": "2000-01-01" }]""";
    private const string FileField = "file";
    private const string DataField = "data";

    /// <inheritdoc/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var payloadNames = context.ApiDescription.ParameterDescriptions
            .Where(parameter => parameter.Type == typeof(EmployeeImportPayload))
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.Ordinal);
        if (payloadNames.Count == 0)
        {
            return;
        }

        if (operation.Parameters is { } parameters)
        {
            foreach (var parameter in parameters.Where(parameter => parameter.Name is { } name && payloadNames.Contains(name)).ToList())
            {
                parameters.Remove(parameter);
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description = "CSV(헤더 없음, 열 순서 name,email,tel,joined) 또는 JSON(배열 · 단일 객체 · 대괄호 없는 나열) 입력. "
                + "multipart는 file · data 중 하나만 보냅니다(둘 다 · 둘 다 없음은 400). 본문 전체 1 MiB, 최대 1,000행. 문자 인코딩은 UTF-8(BOM 허용).",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [EmployeeController.RegisterContentTypes[0]] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            [FileField] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "binary",
                                Description = "CSV · JSON 파일. 형식은 파트 Content-Type → 확장자(.csv · .json) → 내용 순으로 판별합니다.",
                            },
                            [DataField] = TextField(),
                        },
                    },
                },
                [EmployeeController.RegisterContentTypes[1]] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema> { [DataField] = TextField() },
                    },
                },
                [EmployeeController.RegisterContentTypes[2]] = new()
                {
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Description = "raw CSV 본문." },
                    Example = JsonValue.Create(CsvExample),
                },
                [EmployeeController.RegisterContentTypes[3]] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Description = "raw JSON 본문. 배열 외에 단일 객체 · 대괄호 없는 나열(`{...},{...}`)도 받습니다. 속성 이름은 대소문자를 무시합니다.",
                        Items = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>
                            {
                                ["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                ["email"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                ["tel"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                ["joined"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "yyyy-MM-dd" },
                            },
                        },
                    },
                    Example = JsonNode.Parse(JsonExample),
                },
            },
        };
    }

    private static OpenApiSchema TextField() => new()
    {
        Type = JsonSchemaType.String,
        Description = "CSV · JSON 텍스트. 형식은 내용으로 판별합니다([ · {로 시작하면 JSON).",
        Example = JsonValue.Create(CsvExample),
    };
}
