using EmergencyHub.BuildingBlocks.Api.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.OpenApi;

// ADR-0019 "실패 응답": ProblemDetails 스키마에 확장 필드 code(정수) · traceId · errors가 드러나야 한다.
public sealed class ProblemDetailsSchemaFilterTests
{
    private readonly ProblemDetailsSchemaFilter _filter = new();

    // ---- 성공 ----

    [Fact]
    public void Apply_ProblemDetails_AddsIntegerCodeAndStringTraceId()
    {
        var schema = CreateObjectSchema();

        _filter.Apply(schema, CreateContext(typeof(ProblemDetails)));

        schema.Properties!["code"].Type.Should().Be(JsonSchemaType.Integer);
        schema.Properties["code"].Format.Should().Be("int32");
        schema.Properties["traceId"].Type.Should().Be(JsonSchemaType.String);
        schema.Required.Should().Contain(["code", "traceId"]);
    }

    [Fact]
    public void Apply_ProblemDetails_AddsErrorsAsMapOfFieldErrorArrays()
    {
        var schema = CreateObjectSchema();

        _filter.Apply(schema, CreateContext(typeof(ProblemDetails)));

        var errors = schema.Properties!["errors"];
        errors.Type.Should().Be(JsonSchemaType.Object);
        var fieldErrors = errors.AdditionalProperties!;
        fieldErrors.Type.Should().Be(JsonSchemaType.Array);
        fieldErrors.Items!.Properties!["code"].Type.Should().Be(JsonSchemaType.Integer);
        fieldErrors.Items.Properties["message"].Type.Should().Be(JsonSchemaType.String);
        schema.Required.Should().NotContain("errors");
    }

    // ---- 실패 ----

    [Fact]
    public void Apply_OtherType_LeavesSchemaUnchanged()
    {
        var schema = CreateObjectSchema();

        _filter.Apply(schema, CreateContext(typeof(SampleResponse)));

        schema.Properties.Should().BeEmpty();
    }

    [Fact]
    public void Apply_NullArguments_ThrowArgumentNullException()
    {
        var nullSchema = () => _filter.Apply(null!, CreateContext(typeof(ProblemDetails)));
        var nullContext = () => _filter.Apply(CreateObjectSchema(), null!);

        nullSchema.Should().Throw<ArgumentNullException>().WithParameterName("schema");
        nullContext.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    // ---- 엣지 ----

    [Fact]
    public void Apply_Twice_DoesNotDuplicateOrThrow()
    {
        var schema = CreateObjectSchema();
        var context = CreateContext(typeof(ProblemDetails));

        _filter.Apply(schema, context);
        _filter.Apply(schema, context);

        schema.Properties!.Keys.Should().BeEquivalentTo(["code", "traceId", "errors"]);
        schema.Required!.Where(name => name == "code").Should().ContainSingle();
    }

    private static OpenApiSchema CreateObjectSchema() => new()
    {
        Type = JsonSchemaType.Object,
        Properties = new Dictionary<string, IOpenApiSchema>(),
    };

    private static SchemaFilterContext CreateContext(Type type) =>
        new(type, Substitute.For<ISchemaGenerator>(), new SchemaRepository());
}
