using System.Globalization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmergencyHub.BuildingBlocks.Api.OpenApi;

/// <summary>
/// enum 스키마(정수)에 값 = 이름 대응을 설명으로 붙입니다(ADR-0019 "코드값"). 스키마 형식은 정수 그대로 둡니다.
/// </summary>
/// <remarks>
/// 예: <c>0 = Unknown, 1 = Active, 3 = Retired</c>. <c>[Flags]</c> enum은 앞에 <c>비트 플래그(합으로 조합):</c>를 붙입니다.
/// 값은 작은 값부터 적습니다(부호 있는 64비트 비교). 기존 설명이 있으면 뒤에 괄호로 덧붙입니다.
/// </remarks>
internal sealed class IntegerEnumSchemaFilter : ISchemaFilter
{
    private const string FlagsPrefix = "비트 플래그(합으로 조합): ";

    /// <inheritdoc/>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        if (!type.IsEnum || schema is not OpenApiSchema concrete)
        {
            return;
        }

        var values = Describe(type);
        concrete.Description = string.IsNullOrWhiteSpace(concrete.Description) ? values : $"{concrete.Description} ({values})";
    }

    private static string Describe(Type enumType)
    {
        var pairs = Enum.GetNames(enumType)
            .Select(name => (Name: name, Value: Convert.ToInt64(Enum.Parse(enumType, name), CultureInfo.InvariantCulture)))
            .OrderBy(pair => pair.Value)
            .Select(pair => $"{pair.Value.ToString(CultureInfo.InvariantCulture)} = {pair.Name}");

        var described = string.Join(", ", pairs);
        return enumType.IsDefined(typeof(FlagsAttribute), inherit: false) ? FlagsPrefix + described : described;
    }
}
