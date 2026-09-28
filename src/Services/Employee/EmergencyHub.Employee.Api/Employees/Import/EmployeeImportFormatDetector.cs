using System.Buffers;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using Microsoft.Net.Http.Headers;

namespace EmergencyHub.Employee.Api.Employees.Import;

/// <summary>
/// 일괄 등록 입력의 내용 형식 판별입니다(ADR-0026 2절, PRD-002 FR-05). 판별 순서는 Content-Type → 파일 확장자 → 내용 추정입니다.
/// </summary>
/// <remarks>
/// 내용을 해석(파싱 · 검증)하지 않습니다. 입력이 비어 있으면(맨 앞 BOM 하나를 뗀 뒤 0x20 · 0x09 · 0x0D · 0x0A만, Validator의 빈 입력 정의와 같음)
/// 형식을 정하지 않고 <see cref="EmployeeImportFormat.Unknown"/>을 돌려줍니다. Validator가 빈 입력(21028)을 먼저 보고합니다.
/// </remarks>
internal static class EmployeeImportFormatDetector
{
    private const string CsvMediaType = "text/csv";
    private const string JsonMediaType = "application/json";

    private static readonly SearchValues<byte> BlankBytes = SearchValues.Create([0x20, 0x09, 0x0D, 0x0A]);

    private static ReadOnlySpan<byte> Bom => [0xEF, 0xBB, 0xBF];

    /// <summary>세 단계를 순서대로 적용합니다.</summary>
    /// <param name="contentType">raw body면 요청 Content-Type, multipart 파일 필드면 그 파트의 Content-Type. 텍스트 필드 <c>data</c>는 <see langword="null"/>.</param>
    /// <param name="fileName">multipart 파일 필드의 파일 이름. 그 밖은 <see langword="null"/>.</param>
    /// <param name="content">입력 바이트.</param>
    /// <returns>판별한 형식. 입력이 비었으면 <see cref="EmployeeImportFormat.Unknown"/>.</returns>
    public static EmployeeImportFormat Detect(string? contentType, string? fileName, ReadOnlySpan<byte> content)
    {
        if (IsBlank(content))
        {
            return EmployeeImportFormat.Unknown;
        }

        var format = FromMediaType(contentType);
        if (format == EmployeeImportFormat.Unknown)
        {
            format = FromFileName(fileName);
        }

        return format == EmployeeImportFormat.Unknown ? FromContent(content) : format;
    }

    /// <summary>1단계: <c>text/csv</c> → Csv, <c>application/json</c> → Json(매개변수 · 대소문자 무시).</summary>
    /// <param name="contentType">Content-Type 헤더 값.</param>
    /// <returns>판별한 형식. 그 밖이거나 없거나 해석할 수 없으면 <see cref="EmployeeImportFormat.Unknown"/>.</returns>
    public static EmployeeImportFormat FromMediaType(string? contentType)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
        {
            return EmployeeImportFormat.Unknown;
        }

        if (mediaType.MediaType.Equals(CsvMediaType, StringComparison.OrdinalIgnoreCase))
        {
            return EmployeeImportFormat.Csv;
        }

        return mediaType.MediaType.Equals(JsonMediaType, StringComparison.OrdinalIgnoreCase)
            ? EmployeeImportFormat.Json
            : EmployeeImportFormat.Unknown;
    }

    /// <summary>2단계: 파일 이름 확장자 <c>.csv</c> → Csv, <c>.json</c> → Json(대소문자 무시).</summary>
    /// <param name="fileName">파일 이름.</param>
    /// <returns>판별한 형식. 그 밖이거나 없으면 <see cref="EmployeeImportFormat.Unknown"/>.</returns>
    public static EmployeeImportFormat FromFileName(string? fileName)
    {
        var extension = Path.GetExtension(fileName.AsSpan());
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return EmployeeImportFormat.Csv;
        }

        return extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? EmployeeImportFormat.Json
            : EmployeeImportFormat.Unknown;
    }

    /// <summary>3단계: 맨 앞 BOM 하나와 앞 공백(0x20 · 0x09 · 0x0D · 0x0A)을 건너뛴 첫 바이트가 <c>[</c> · <c>{</c>이면 Json, 그 밖이면 Csv.</summary>
    /// <param name="content">입력 바이트.</param>
    /// <returns>판별한 형식. 건너뛴 뒤 남은 바이트가 없으면 <see cref="EmployeeImportFormat.Unknown"/>.</returns>
    public static EmployeeImportFormat FromContent(ReadOnlySpan<byte> content)
    {
        var body = WithoutBom(content);
        var first = body.IndexOfAnyExcept(BlankBytes);
        if (first < 0)
        {
            return EmployeeImportFormat.Unknown;
        }

        return body[first] is (byte)'[' or (byte)'{' ? EmployeeImportFormat.Json : EmployeeImportFormat.Csv;
    }

    private static bool IsBlank(ReadOnlySpan<byte> content) => WithoutBom(content).IndexOfAnyExcept(BlankBytes) < 0;

    private static ReadOnlySpan<byte> WithoutBom(ReadOnlySpan<byte> content) =>
        content.StartsWith(Bom) ? content[Bom.Length..] : content;
}
