using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>
/// 일괄 등록(<c>POST /api/employee</c>) 요청 본문을 입력 경로(PRD-002 FR-05 입력 경로 표)별로 만듭니다(S06-T06 통합 · 인수 테스트).
/// </summary>
/// <remarks>
/// 본문 바이트는 그대로 싣습니다(잘못된 UTF-8 · NUL 포함). form-urlencoded는 바이트 단위로 퍼센트 인코딩하므로 잘못된 UTF-8도 디코딩 뒤 같은 바이트가 됩니다.
/// </remarks>
public static class ImportContent
{
    /// <summary>일괄 등록 경로입니다(쿼리 문자열 없음, ProblemDetails <c>instance</c>).</summary>
    public const string RegisterPath = "/api/employee";

    /// <summary>등록 경로 URI(상대)입니다.</summary>
    public static Uri RegisterUri { get; } = new(RegisterPath, UriKind.Relative);

    /// <summary>입력 경로 이름 네 가지입니다(FR-05 입력 경로 표의 201 행).</summary>
    public static IReadOnlyList<string> Paths { get; } = ["multipart-file", "multipart-data", "form-data", "raw"];

    /// <summary>multipart <c>file</c> 필드(파일 이름 있음)입니다. 브라우저 <c>&lt;input type=file&gt;</c>에 해당합니다.</summary>
    /// <param name="bytes">파일 바이트.</param>
    /// <param name="fileName">파일 이름(확장자로 형식을 판별할 수 있음).</param>
    /// <param name="contentType">파일 부분 Content-Type. <see langword="null"/>이면 헤더를 넣지 않습니다.</param>
    /// <returns>본문.</returns>
    public static MultipartFormDataContent MultipartFile(byte[] bytes, string fileName, string? contentType = "application/octet-stream")
    {
        var file = new ByteArrayContent(bytes);
        if (contentType is not null)
        {
            file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        }

        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    /// <summary>multipart <c>data</c> 텍스트 필드(파일 이름 없음)입니다. 브라우저 <c>&lt;textarea name=data&gt;</c>에 해당합니다.</summary>
    /// <param name="bytes">텍스트 바이트.</param>
    /// <returns>본문.</returns>
    public static MultipartFormDataContent MultipartData(byte[] bytes) => new() { { new ByteArrayContent(bytes), "data" } };

    /// <summary><c>application/x-www-form-urlencoded</c>의 <c>data</c> 키입니다(바이트 단위 퍼센트 인코딩).</summary>
    /// <param name="bytes">텍스트 바이트.</param>
    /// <returns>본문.</returns>
    public static ByteArrayContent FormData(byte[] bytes) => FormUrlEncoded("data=" + PercentEncode(bytes));

    /// <summary><c>application/x-www-form-urlencoded</c> 본문을 그대로 싣습니다(키 없음 · 같은 키 두 번 등).</summary>
    /// <param name="body">이미 인코딩한 본문(ASCII).</param>
    /// <returns>본문.</returns>
    public static ByteArrayContent FormUrlEncoded(string body)
    {
        var content = new ByteArrayContent(Encoding.ASCII.GetBytes(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        return content;
    }

    /// <summary>raw body입니다.</summary>
    /// <param name="bytes">본문 바이트.</param>
    /// <param name="contentType">Content-Type(예: <c>text/csv</c>).</param>
    /// <returns>본문.</returns>
    public static ByteArrayContent Raw(byte[] bytes, string contentType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        return content;
    }

    /// <summary>입력 경로 이름으로 본문을 만듭니다(<see cref="Paths"/>).</summary>
    /// <param name="path">
    /// <c>multipart-file</c> · <c>multipart-data</c> · <c>form-data</c> · <c>raw</c>. 파일 이름 · raw Content-Type은 <paramref name="json"/>으로 고릅니다.
    /// </param>
    /// <param name="bytes">본문 바이트.</param>
    /// <param name="json"><see langword="true"/>면 JSON(<c>employees.json</c> · <c>application/json</c>), 아니면 CSV.</param>
    /// <returns>본문.</returns>
    /// <exception cref="ArgumentOutOfRangeException">경로 이름이 목록에 없는 경우.</exception>
    public static HttpContent For(string path, byte[] bytes, bool json) => path switch
    {
        "multipart-file" => MultipartFile(bytes, json ? "employees.json" : "employees.csv"),
        "multipart-data" => MultipartData(bytes),
        "form-data" => FormData(bytes),
        "raw" => Raw(bytes, json ? "application/json" : "text/csv"),
        _ => throw new ArgumentOutOfRangeException(nameof(path), path, "알 수 없는 입력 경로입니다."),
    };

    /// <summary>JSON 배열 본문에서 바깥 대괄호를 뗀 "대괄호 없는 나열"을 만듭니다(원문 <c>json ex)</c> 모양).</summary>
    /// <param name="jsonArray">JSON 배열 바이트(<c>[</c>로 시작, <c>]</c>로 끝남).</param>
    /// <returns>나열 바이트.</returns>
    public static byte[] WithoutBrackets(byte[] jsonArray)
    {
        ArgumentNullException.ThrowIfNull(jsonArray);

        return jsonArray[1..^1];
    }

    private static string PercentEncode(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 3);
        foreach (var value in bytes)
        {
            var isUnreserved = value is (>= (byte)'A' and <= (byte)'Z') or (>= (byte)'a' and <= (byte)'z') or (>= (byte)'0' and <= (byte)'9')
                or (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~';
            builder.Append(isUnreserved ? ((char)value).ToString() : string.Create(CultureInfo.InvariantCulture, $"%{value:X2}"));
        }

        return builder.ToString();
    }
}
