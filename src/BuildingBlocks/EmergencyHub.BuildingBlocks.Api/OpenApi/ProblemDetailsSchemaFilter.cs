using EmergencyHub.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmergencyHub.BuildingBlocks.Api.OpenApi;

/// <summary>
/// <see cref="ProblemDetails"/> 스키마에 확장 필드 <c>code</c>(정수) · <c>traceId</c> · <c>errors</c>를 드러냅니다(ADR-0019 "실패 응답").
/// </summary>
/// <remarks>
/// <c>code</c> · <c>traceId</c>는 항상 들어가므로 필수로 표시하고, <c>errors</c>는 검증 실패(400)와 상세 충돌(409, ADR-0028)에만 있어 선택입니다.
/// <c>errors</c>는 필드 키 → <see cref="ProblemFieldError"/> 배열의 맵입니다.
/// </remarks>
internal sealed class ProblemDetailsSchemaFilter : ISchemaFilter
{
    /// <inheritdoc/>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Type != typeof(ProblemDetails) || schema is not OpenApiSchema concrete)
        {
            return;
        }

        concrete.Properties ??= new Dictionary<string, IOpenApiSchema>();
        concrete.Properties[ErrorProblemDetails.CodeExtension] = new OpenApiSchema
        {
            Type = JsonSchemaType.Integer,
            Format = "int32",
            Description = "정수 에러 코드(S T NNN). 클라이언트는 이 값으로 분기합니다.",
        };
        concrete.Properties[ErrorProblemDetails.TraceIdExtension] = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description = "추적 ID(W3C trace-id 32자리 16진수, 없으면 요청 식별자).",
        };
        concrete.Properties[ErrorProblemDetails.ErrorsExtension] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description = "검증 실패(400)의 필드별 오류, 상세 충돌(409)의 항목별 충돌. 키는 camelCase 필드 경로입니다.",
            AdditionalProperties = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["code"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
                        ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    },
                    Required = new HashSet<string> { "code", "message" },
                },
            },
        };

        concrete.Required ??= new HashSet<string>();
        concrete.Required.Add(ErrorProblemDetails.CodeExtension);
        concrete.Required.Add(ErrorProblemDetails.TraceIdExtension);
    }
}
