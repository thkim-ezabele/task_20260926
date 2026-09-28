using System.Net;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace EmergencyHub.Employee.Api.Employees.Import;

/// <summary>
/// 일괄 등록 요청의 전송 형식에서 입력 바이트 · 형식 · 출처만 꺼내 <see cref="EmployeeImportPayload"/>를 만드는 전용 바인더입니다
/// (ADR-0026 1 · 2절, PRD-002 FR-05 · NFR-01). 내용을 해석(파싱 · 검증)하지 않습니다.
/// </summary>
/// <remarks>
/// <para>입력 경로(요청 Content-Type 기준):</para>
/// <list type="bullet">
/// <item><c>multipart/form-data</c>: 파일 필드 <c>file</c>(filename 있는 파트) → <see cref="EmployeeImportSources.File"/>, 텍스트 필드 <c>data</c> → <see cref="EmployeeImportSources.Data"/>.</item>
/// <item><c>application/x-www-form-urlencoded</c>: 텍스트 필드 <c>data</c> → <see cref="EmployeeImportSources.Data"/>.</item>
/// <item><c>text/csv</c> · <c>application/json</c> · Content-Type 없음: raw body → <see cref="EmployeeImportSources.Body"/>.
/// Content-Type이 없으면 415로 거절하지 않고 내용으로 판별합니다(ADR-0026 2절 "없으면 다음 단계", S06-T01 결정의 판정 결과).</item>
/// </list>
/// <para>
/// 찾은 출처 비트를 모두 켜고, Content는 file → data 순서로 먼저 찾은 값입니다(조합 판정은 Validator). 필드 이름은 대소문자를 무시하고(프레임워크 폼과 같음)
/// 그 밖의 필드는 무시합니다. 형식은 <see cref="EmployeeImportFormatDetector"/>로 판별합니다.
/// </para>
/// <para>
/// <b>바이트 보존</b>: 프레임워크 폼 해독(<c>ReadFormAsync</c>)을 쓰지 않고 본문을 직접 읽어 multipart는 <see cref="MultipartReader"/>로,
/// form-urlencoded는 퍼센트 해독만 해 원래 바이트를 넘깁니다. 프레임워크 해독은 잘못된 UTF-8을 바꾸므로(S06-T05 ④ 실측) 21022 판정을
/// Application 해독 단계 한 곳에 두기 위해서입니다. 액션에는 <see cref="DisableFormValueProvidersAttribute"/>가 있어야 합니다(폼 값 공급자가 먼저 본문을 읽지 않도록).
/// </para>
/// <para>판정(S06-T05, api-guidelines 규칙 예외 절 "일괄 등록 크기 한도 · 전송 형식 오류 판정"):</para>
/// <list type="bullet">
/// <item><b>413 · 1004</b>: 본문 바이트 수가 <see cref="IRequestSizeLimitMetadata"/>(<c>RequestSizeLimit</c>)를, multipart 본문이 <c>MultipartBodyLengthLimit</c>를,
/// 텍스트 필드 <c>data</c> 값이 <c>ValueLengthLimit</c>(<see cref="IFormOptionsMetadata"/>)를 넘으면 서버(Kestrel)와 같은
/// <see cref="BadHttpRequestException"/>(413)을 던집니다. 전역 예외 처리기가 1004로 바꿉니다(ADR-0028). Kestrel이 먼저 한도를 적용하면 본문을 읽는 중에 같은 예외가 납니다.
/// 예외 메시지로 판정하지 않고 바이트 수로 판정합니다.</item>
/// <item><b>400 · 1001</b>: 잘못된 전송 형식(boundary 없음 · 빈 boundary, 잘리거나 boundary가 없는 multipart, multipart 헤더 수 · 길이 한도 위반, 같은 필드 두 번)은
/// ModelState 오류(키 <c>""</c>, 예외 · 원문 없음) → <c>[ApiController]</c> 자동 400.</item>
/// <item><b>415 · 1005</b>: 그 밖의 Content-Type은 ModelState의 <see cref="UnsupportedContentTypeException"/> → <c>UnsupportedContentTypeFilter</c> → 공통 415.
/// 라우팅 <c>[Consumes]</c>가 먼저 거르므로 실제 요청에는 생기지 않는 방어 경로입니다.</item>
/// </list>
/// </remarks>
internal sealed class EmployeeImportPayloadBinder : IModelBinder
{
    private const string FileFieldName = "file";
    private const string DataFieldName = "data";
    private const string MultipartFormData = "multipart/form-data";
    private const string FormUrlEncoded = "application/x-www-form-urlencoded";
    private const int ReadChunkSize = 16 * 1024;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="bindingContext"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="BadHttpRequestException">한도를 넘은 경우(413).</exception>
    /// <exception cref="InvalidOperationException">액션에 <c>RequestSizeLimit</c> · <c>RequestFormLimits</c> 한도가 없는 경우(설정 오류).</exception>
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var httpContext = bindingContext.HttpContext;
        var limits = ImportRequestLimits.From(httpContext.GetEndpoint());
        var request = httpContext.Request;

        var transport = TransportOf(request.ContentType);
        if (transport == ImportTransport.Unsupported)
        {
            bindingContext.ModelState.TryAddModelException(string.Empty, new UnsupportedContentTypeException("일괄 등록이 지원하지 않는 Content-Type입니다."));
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        var body = await ReadBodyAsync(request, limits.MaxRequestBodySize, httpContext.RequestAborted);
        var payload = transport switch
        {
            ImportTransport.Multipart => (await ReadMultipartAsync(request.ContentType, body, limits, httpContext.RequestAborted))?.ToPayload(limits),
            ImportTransport.FormUrlEncoded => ReadFormUrlEncoded(body)?.ToPayload(limits),
            _ => new EmployeeImportPayload(EmployeeImportFormatDetector.Detect(request.ContentType, null, body), EmployeeImportSources.Body, body),
        };

        if (payload is null)
        {
            // 값 · 예외 메시지를 싣지 않는다. 응답은 1001 고정 문구다(InvalidModelStateResponses).
            bindingContext.ModelState.TryAddModelError(string.Empty, "일괄 등록 요청의 전송 형식이 올바르지 않습니다.");
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        bindingContext.Result = ModelBindingResult.Success(payload);
    }

    private static ImportTransport TransportOf(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return ImportTransport.Raw;
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var parsed))
        {
            return ImportTransport.Unsupported;
        }

        if (parsed.MediaType.Equals(MultipartFormData, StringComparison.OrdinalIgnoreCase))
        {
            return ImportTransport.Multipart;
        }

        if (parsed.MediaType.Equals(FormUrlEncoded, StringComparison.OrdinalIgnoreCase))
        {
            return ImportTransport.FormUrlEncoded;
        }

        return EmployeeImportFormatDetector.FromMediaType(contentType) == EmployeeImportFormat.Unknown
            ? ImportTransport.Unsupported
            : ImportTransport.Raw;
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequest request, long limit, CancellationToken cancellationToken)
    {
        if (request.ContentLength > limit)
        {
            throw PayloadTooLarge();
        }

        // Kestrel은 RequestSizeLimit을 넘으면 읽는 중에 같은 예외(413)를 던진다. TestServer 등 한도를 적용하지 않는 서버를 위해 직접도 센다.
        using var buffer = new MemoryStream();
        var chunk = new byte[ReadChunkSize];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > limit)
            {
                throw PayloadTooLarge();
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static async Task<FormFields?> ReadMultipartAsync(
        string? contentType, byte[] body, ImportRequestLimits limits, CancellationToken cancellationToken)
    {
        if (body.Length > limits.MultipartBodyLengthLimit)
        {
            throw PayloadTooLarge();
        }

        var boundary = MediaTypeHeaderValue.TryParse(contentType, out var mediaType) ? HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value : null;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            return null;
        }

        using var stream = new MemoryStream(body, writable: false);
        var reader = new MultipartReader(boundary, stream) { BodyLengthLimit = limits.MultipartBodyLengthLimit };
        ImportFile? file = null;
        byte[]? data = null;
        try
        {
            while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
            {
                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition))
                {
                    continue;
                }

                var name = HeaderUtilities.RemoveQuotes(disposition.Name).Value;
                if (disposition.IsFileDisposition() && string.Equals(name, FileFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    if (file is not null)
                    {
                        return null;
                    }

                    file = new ImportFile(await ReadSectionAsync(section, cancellationToken), FileNameOf(disposition), section.ContentType);
                }
                else if (disposition.IsFormDisposition() && string.Equals(name, DataFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    if (data is not null)
                    {
                        return null;
                    }

                    data = await ReadSectionAsync(section, cancellationToken);
                }
            }
        }
        catch (InvalidDataException)
        {
            // 메모리 본문이라 입출력 예외는 전송 형식 오류뿐이다(끊긴 연결은 본문을 읽는 단계에서 이미 난다).
            return null;
        }
        catch (IOException)
        {
            return null;
        }

        return new FormFields(file, data);
    }

    private static async Task<byte[]> ReadSectionAsync(MultipartSection section, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await section.Body.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static string? FileNameOf(ContentDispositionHeaderValue disposition)
    {
        var fileNameStar = HeaderUtilities.RemoveQuotes(disposition.FileNameStar);
        return fileNameStar.HasValue && fileNameStar.Length > 0
            ? fileNameStar.Value
            : HeaderUtilities.RemoveQuotes(disposition.FileName).Value;
    }

    private static FormFields? ReadFormUrlEncoded(byte[] bytes)
    {
        byte[]? data = null;
        var start = 0;
        while (start <= bytes.Length)
        {
            var end = Array.IndexOf(bytes, (byte)'&', start);
            if (end < 0)
            {
                end = bytes.Length;
            }

            if (end > start)
            {
                var equals = Array.IndexOf(bytes, (byte)'=', start, end - start);
                var keyEnd = equals < 0 ? end : equals;
                if (IsDataKey(bytes, start, keyEnd - start))
                {
                    if (data is not null)
                    {
                        return null;
                    }

                    data = equals < 0 ? [] : WebUtility.UrlDecodeToBytes(bytes, equals + 1, end - equals - 1);
                }
            }

            start = end + 1;
        }

        return new FormFields(null, data);
    }

    private static bool IsDataKey(byte[] bytes, int offset, int count)
    {
        var key = WebUtility.UrlDecodeToBytes(bytes, offset, count);
        return key.Length == DataFieldName.Length
            && System.Text.Encoding.ASCII.GetString(key).Equals(DataFieldName, StringComparison.OrdinalIgnoreCase);
    }

    private static BadHttpRequestException PayloadTooLarge() =>
        new("일괄 등록 요청 본문이 허용 크기를 넘었습니다.", StatusCodes.Status413PayloadTooLarge);

    /// <summary>요청 Content-Type으로 정한 전송 형식입니다(바인더 안에서만 씀, 저장 · 전송하지 않음).</summary>
    private enum ImportTransport : short
    {
        /// <summary>지원하지 않는 Content-Type(415).</summary>
        Unsupported = 0,

        /// <summary>raw body(<c>text/csv</c> · <c>application/json</c> · Content-Type 없음).</summary>
        Raw = 1,

        /// <summary><c>multipart/form-data</c>.</summary>
        Multipart = 2,

        /// <summary><c>application/x-www-form-urlencoded</c>.</summary>
        FormUrlEncoded = 3,
    }

    /// <summary>액션 특성에서 읽은 한도입니다.</summary>
    private sealed record ImportRequestLimits(long MaxRequestBodySize, long MultipartBodyLengthLimit, int ValueLengthLimit)
    {
        public static ImportRequestLimits From(Endpoint? endpoint)
        {
            var sizeLimit = endpoint?.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize;
            var formOptions = endpoint?.Metadata.GetMetadata<IFormOptionsMetadata>();
            if (sizeLimit is not { } maxRequestBodySize
                || formOptions?.MultipartBodyLengthLimit is not { } multipartBodyLengthLimit
                || formOptions.ValueLengthLimit is not { } valueLengthLimit)
            {
                throw new InvalidOperationException(
                    "일괄 등록 바인더를 쓰는 액션에는 RequestSizeLimit과 RequestFormLimits(MultipartBodyLengthLimit · ValueLengthLimit)가 있어야 합니다.");
            }

            return new ImportRequestLimits(maxRequestBodySize, multipartBodyLengthLimit, valueLengthLimit);
        }
    }

    /// <summary>폼에서 찾은 파일 필드입니다.</summary>
    private sealed record ImportFile(byte[] Content, string? FileName, string? ContentType);

    /// <summary>폼에서 찾은 필드입니다. 없는 필드는 <see langword="null"/>입니다.</summary>
    private sealed record FormFields(ImportFile? File, byte[]? Data)
    {
        /// <summary><c>data</c> 값 한도를 확인하고 입력을 만듭니다.</summary>
        /// <exception cref="BadHttpRequestException"><c>data</c> 값이 <c>ValueLengthLimit</c>를 넘은 경우(413).</exception>
        public EmployeeImportPayload ToPayload(ImportRequestLimits limits)
        {
            if (Data?.Length > limits.ValueLengthLimit)
            {
                throw PayloadTooLarge();
            }

            var sources = (File is null ? EmployeeImportSources.None : EmployeeImportSources.File)
                | (Data is null ? EmployeeImportSources.None : EmployeeImportSources.Data);

            if (File is not null)
            {
                return new EmployeeImportPayload(EmployeeImportFormatDetector.Detect(File.ContentType, File.FileName, File.Content), sources, File.Content);
            }

            return Data is not null
                ? new EmployeeImportPayload(EmployeeImportFormatDetector.Detect(null, null, Data), sources, Data)
                : new EmployeeImportPayload(EmployeeImportFormat.Unknown, sources, ReadOnlyMemory<byte>.Empty);
        }
    }
}
