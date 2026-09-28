using EmergencyHub.BuildingBlocks.Api.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.OpenApi;

// ADR-0019 "코드값": enum 스키마는 정수로 두고, 값 = 이름 대응을 스키마 설명에 넣는다(문자열 enum 스키마로 바꾸지 않음).
public sealed class IntegerEnumSchemaFilterTests
{
    private readonly IntegerEnumSchemaFilter _filter = new();

    // ---- 성공 ----

    [Fact]
    public void Apply_CodeEnum_DescribesEachValueAndName()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };

        _filter.Apply(schema, CreateContext(typeof(SampleStatus)));

        schema.Description.Should().Be("0 = Unknown, 1 = Active, 3 = Retired");
        schema.Type.Should().Be(JsonSchemaType.Integer);
    }

    [Fact]
    public void Apply_FlagsEnum_MarksBitFlagsAndListsBitsAndAliases()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };

        _filter.Apply(schema, CreateContext(typeof(SampleChannels)));

        schema.Description.Should().Be("비트 플래그(합으로 조합): 0 = None, 1 = Sms, 2 = Push, 4 = Email, 7 = All");
    }

    [Fact]
    public void Apply_ExistingDescription_IsKeptAndValuesAppended()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Description = "직원 상태" };

        _filter.Apply(schema, CreateContext(typeof(SampleStatus)));

        schema.Description.Should().Be("직원 상태 (0 = Unknown, 1 = Active, 3 = Retired)");
    }

    // ---- 실패 ----

    [Fact]
    public void Apply_NonEnumType_LeavesSchemaUnchanged()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };

        _filter.Apply(schema, CreateContext(typeof(string)));

        schema.Description.Should().BeNull();
    }

    [Fact]
    public void Apply_NullArguments_ThrowArgumentNullException()
    {
        var nullSchema = () => _filter.Apply(null!, CreateContext(typeof(SampleStatus)));
        var nullContext = () => _filter.Apply(new OpenApiSchema(), null!);

        nullSchema.Should().Throw<ArgumentNullException>().WithParameterName("schema");
        nullContext.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    // ---- 엣지 ----

    [Fact]
    public void Apply_NullableEnum_DescribesUnderlyingEnum()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };

        _filter.Apply(schema, CreateContext(typeof(SampleStatus?)));

        schema.Description.Should().Be("0 = Unknown, 1 = Active, 3 = Retired");
    }

    [Fact]
    public void Apply_SignedAndLargeValues_UseInvariantDigitsInValueOrder()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };

        _filter.Apply(schema, CreateContext(typeof(SignedSampleCode)));

        schema.Description.Should().Be("-1 = Negative, 0 = Unknown, 1099511627776 = Large");
    }

    private static SchemaFilterContext CreateContext(Type type) =>
        new(type, Substitute.For<ISchemaGenerator>(), new SchemaRepository());
}
