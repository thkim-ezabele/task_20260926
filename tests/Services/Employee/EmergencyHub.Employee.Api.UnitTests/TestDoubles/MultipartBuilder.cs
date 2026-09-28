using System.Text;

namespace EmergencyHub.Employee.Api.UnitTests.TestDoubles;

/// <summary>
/// multipart/form-data 본문을 바이트 그대로 만듭니다. 파트 본문을 문자열로 바꾸지 않으므로 잘못된 UTF-8 바이트를 그대로 담을 수 있습니다.
/// </summary>
internal sealed class MultipartBuilder
{
    /// <summary>본문의 boundary입니다. Content-Type은 <c>multipart/form-data; boundary=emergency-hub-boundary</c>.</summary>
    public const string Boundary = "emergency-hub-boundary";

    private readonly List<byte> _body = [];

    /// <summary>텍스트 필드(filename 없는 form-data)를 더합니다.</summary>
    public MultipartBuilder Field(string name, byte[] content, string? contentType = null) =>
        RawPart($"Content-Disposition: form-data; name=\"{name}\"" + (contentType is null ? string.Empty : $"\r\nContent-Type: {contentType}"), content);

    /// <summary>파일 필드(filename 있는 form-data)를 더합니다.</summary>
    public MultipartBuilder File(string name, string fileName, string? contentType, byte[] content) =>
        RawPart(
            $"Content-Disposition: form-data; name=\"{name}\"; filename=\"{fileName}\"" + (contentType is null ? string.Empty : $"\r\nContent-Type: {contentType}"),
            content);

    /// <summary>헤더 줄(CRLF로 구분)과 본문으로 파트 하나를 더합니다.</summary>
    public MultipartBuilder RawPart(string headers, byte[] content)
    {
        Write($"--{Boundary}\r\n{headers}\r\n\r\n");
        _body.AddRange(content);
        Write("\r\n");
        return this;
    }

    /// <summary>닫는 boundary를 붙인 본문을 돌려줍니다.</summary>
    public byte[] Build()
    {
        Write($"--{Boundary}--\r\n");
        return _body.ToArray();
    }

    private void Write(string text) => _body.AddRange(Encoding.UTF8.GetBytes(text));
}
