using System.Text;
using EmergencyHub.Employee.Api.Employees.Import;
using EmergencyHub.Employee.Api.UnitTests.TestDoubles;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace EmergencyHub.Employee.Api.UnitTests.Employees.Import;

// S06-T05 전용 바인더(ADR-0026 1 · 2절, PRD-002 FR-05 · NFR-01). 바인더는 전송 형식에서 입력 바이트 · 형식 · 출처만 꺼낸다(해석 안 함).
// - 출처: multipart 파일 필드 file(파일 disposition) · 텍스트 필드 data(multipart · form-urlencoded) · raw body(text/csv · application/json · Content-Type 없음).
//   찾은 출처 비트를 모두 켜고(조합 판정은 Validator), Content는 file → data 순서로 먼저 찾은 것. 필드 이름은 대소문자 무시(프레임워크 폼과 같음).
// - 바이트 보존: 프레임워크 폼 해독(문자열)을 쓰지 않고 원래 바이트를 넘긴다. 잘못된 UTF-8은 Application 해독 단계가 21022로 판정한다(S06-T05 ④ 실측).
// - 한도: 액션 메타데이터(RequestSizeLimit · RequestFormLimits)의 값. 넘으면 서버와 같은 BadHttpRequestException(413) → 전역 처리기 413 · 1004.
// - 잘못된 전송 형식(boundary 없음, 잘린 multipart, 같은 필드 두 번)은 ModelState 오류(키 "") → 400 · 1001. 지원하지 않는 Content-Type은
//   ModelState의 UnsupportedContentTypeException → 415 · 1005(라우팅 [Consumes]를 지난 요청에는 생기지 않는 방어 경로).
[Trait("FR", "PRD-002/FR-05")]
[Trait("NFR", "PRD-002/NFR-01")]
public sealed class EmployeeImportPayloadBinderTests
{
    private const string Boundary = MultipartBuilder.Boundary;
    private const string MultipartType = "multipart/form-data; boundary=" + Boundary;
    private const string FormType = "application/x-www-form-urlencoded";
    private const int Limit = 1024;

    private static readonly byte[] CsvRow = Encoding.UTF8.GetBytes("홍길동,hong@example.com,010-1234-5678,2020-01-02");
    private static readonly byte[] JsonArray = Encoding.UTF8.GetBytes("[{\"name\":\"홍길동\",\"email\":\"hong@example.com\"}]");

    // CP949 '홍'(C8 AB), UTF-16 LE BOM처럼 보이는 FF FE, UTF-8로 인코딩한 서로게이트(ED A0 80), 잘린 문자(E4 B8).
    private static readonly byte[] InvalidUtf8 = [0xFF, 0xFE, 0x61, 0xC8, 0xAB, 0xED, 0xA0, 0x80, 0xE4, 0xB8];

    private readonly EmployeeImportPayloadBinder _binder = new();

    // ---- 성공: 입력 경로 표의 네 경로 ----

    [Theory]
    [InlineData("text/csv", EmployeeImportFormat.Csv)]
    [InlineData("application/json", EmployeeImportFormat.Json)]
    [InlineData("text/csv; charset=utf-8", EmployeeImportFormat.Csv)]
    public async Task Bind_RawBody_ReturnsBodySourceWithContentTypeFormat(string contentType, EmployeeImportFormat expected)
    {
        var content = expected == EmployeeImportFormat.Json ? JsonArray : CsvRow;

        var payload = await BindSuccessAsync(contentType, content);

        payload.Sources.Should().Be(EmployeeImportSources.Body);
        payload.Format.Should().Be(expected);
        payload.Content.ToArray().Should().Equal(content);
    }

    [Fact]
    public async Task Bind_RawJsonContentTypeWithCsvContent_ContentTypeWins()
    {
        var payload = await BindSuccessAsync("application/json", CsvRow);

        payload.Format.Should().Be(EmployeeImportFormat.Json);
    }

    [Theory]
    [InlineData("text/csv", "people.bin", EmployeeImportFormat.Csv)]
    [InlineData("application/octet-stream", "people.json", EmployeeImportFormat.Json)]
    [InlineData(null, "people.csv", EmployeeImportFormat.Csv)]
    [InlineData(null, "people.txt", EmployeeImportFormat.Json)]
    public async Task Bind_MultipartFile_ReturnsFileSourceWithPartContentTypeThenExtensionThenContent(
        string? partContentType, string fileName, EmployeeImportFormat expected)
    {
        var body = new MultipartBuilder().File("file", fileName, partContentType, JsonArray).Build();

        var payload = await BindSuccessAsync(MultipartType, body);

        payload.Sources.Should().Be(EmployeeImportSources.File);
        payload.Format.Should().Be(expected);
        payload.Content.ToArray().Should().Equal(JsonArray);
    }

    [Theory]
    [InlineData(false, EmployeeImportFormat.Csv)]
    [InlineData(true, EmployeeImportFormat.Json)]
    public async Task Bind_MultipartData_ReturnsDataSourceWithContentFormat(bool json, EmployeeImportFormat expected)
    {
        var content = json ? JsonArray : CsvRow;
        var body = new MultipartBuilder().Field("data", content).Build();

        var payload = await BindSuccessAsync(MultipartType, body);

        payload.Sources.Should().Be(EmployeeImportSources.Data);
        payload.Format.Should().Be(expected);
        payload.Content.ToArray().Should().Equal(content);
    }

    [Fact]
    public async Task Bind_MultipartDataWithPartContentType_IgnoresPartContentTypeAndUsesContent()
    {
        // ADR-0026 2절: 텍스트 필드 data는 3단계(내용 추정)로만 판별한다.
        var body = new MultipartBuilder().Field("data", JsonArray, "text/csv").Build();

        var payload = await BindSuccessAsync(MultipartType, body);

        payload.Format.Should().Be(EmployeeImportFormat.Json);
    }

    [Fact]
    public async Task Bind_FormUrlEncodedData_DecodesPercentAndPlusToUtf8Bytes()
    {
        var encoded = "data=" + Uri.EscapeDataString("홍길동,hong@example.com,010-1234-5678,2020-01-02").Replace("%20", "+", StringComparison.Ordinal);

        var payload = await BindSuccessAsync(FormType, Encoding.ASCII.GetBytes(encoded));

        payload.Sources.Should().Be(EmployeeImportSources.Data);
        payload.Format.Should().Be(EmployeeImportFormat.Csv);
        payload.Content.ToArray().Should().Equal(CsvRow);
    }

    [Fact]
    public async Task Bind_FormUrlEncodedJsonData_IsJson()
    {
        var encoded = "data=" + Uri.EscapeDataString("{\"name\":\"a\"},{\"name\":\"b\"}");

        var payload = await BindSuccessAsync(FormType, Encoding.ASCII.GetBytes(encoded));

        payload.Format.Should().Be(EmployeeImportFormat.Json);
    }

    [Theory]
    [InlineData("[{}]", EmployeeImportFormat.Json)]
    [InlineData("홍길동,hong@example.com,010-1234-5678,2020-01-02", EmployeeImportFormat.Csv)]
    public async Task Bind_NoContentType_TreatsAsRawBodyAndDetectsFromContent(string content, EmployeeImportFormat expected)
    {
        // T01 결정의 판정 결과: Content-Type 없는 요청은 415가 아니라 raw body 내용 판별(ADR-0026 2절 "없으면 다음 단계").
        var payload = await BindSuccessAsync(null, Encoding.UTF8.GetBytes(content));

        payload.Sources.Should().Be(EmployeeImportSources.Body);
        payload.Format.Should().Be(expected);
    }

    [Fact]
    public async Task Bind_MultipartFileAndData_TurnsOnBothBitsAndUsesFileContent()
    {
        var body = new MultipartBuilder().Field("data", JsonArray).File("file", "people.csv", "text/csv", CsvRow).Build();

        var payload = await BindSuccessAsync(MultipartType, body);

        payload.Sources.Should().Be(EmployeeImportSources.File | EmployeeImportSources.Data);
        payload.Format.Should().Be(EmployeeImportFormat.Csv);
        payload.Content.ToArray().Should().Equal(CsvRow);
    }

    [Fact]
    public async Task Bind_OtherFields_AreIgnored()
    {
        var multipart = new MultipartBuilder().Field("note", JsonArray).Field("data", CsvRow).File("attachment", "a.json", "application/json", JsonArray).Build();
        var form = Encoding.ASCII.GetBytes("x=1&data=abc&y");

        var fromMultipart = await BindSuccessAsync(MultipartType, multipart);
        var fromForm = await BindSuccessAsync(FormType, form);

        fromMultipart.Sources.Should().Be(EmployeeImportSources.Data);
        fromMultipart.Content.ToArray().Should().Equal(CsvRow);
        fromForm.Sources.Should().Be(EmployeeImportSources.Data);
        fromForm.Content.ToArray().Should().Equal(Encoding.ASCII.GetBytes("abc"));
    }

    [Theory]
    [InlineData("FILE", "DATA")]
    [InlineData("File", "Data")]
    public async Task Bind_FieldNames_AreCaseInsensitive(string fileName, string dataName)
    {
        var body = new MultipartBuilder().File(fileName, "a.csv", "text/csv", CsvRow).Field(dataName, CsvRow).Build();

        var payload = await BindSuccessAsync(MultipartType, body);

        payload.Sources.Should().Be(EmployeeImportSources.File | EmployeeImportSources.Data);
    }

    // ---- 실패: 한도 초과 413(BadHttpRequestException) ----

    [Fact]
    public async Task Bind_RawBodyOverRequestSizeLimit_ThrowsPayloadTooLarge()
    {
        var act = () => BindAsync("text/csv", new byte[Limit + 1]);

        (await act.Should().ThrowAsync<BadHttpRequestException>()).Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task Bind_ContentLengthOverLimit_ThrowsBeforeReadingBody()
    {
        using var body = new ThrowingStream();

        var act = () => BindAsync("text/csv", body, contentLength: Limit + 1);

        (await act.Should().ThrowAsync<BadHttpRequestException>()).Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
        body.ReadCount.Should().Be(0);
    }

    [Theory]
    [InlineData("file")]
    [InlineData("data")]
    public async Task Bind_MultipartOverRequestSizeLimit_ThrowsPayloadTooLarge(string field)
    {
        var builder = new MultipartBuilder();
        var body = (field == "file" ? builder.File("file", "a.csv", "text/csv", new byte[Limit]) : builder.Field("data", new byte[Limit])).Build();

        var act = () => BindAsync(MultipartType, body);

        (await act.Should().ThrowAsync<BadHttpRequestException>()).Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task Bind_FormUrlEncodedOverRequestSizeLimit_ThrowsPayloadTooLarge()
    {
        var act = () => BindAsync(FormType, Encoding.ASCII.GetBytes("data=" + new string('a', Limit)));

        (await act.Should().ThrowAsync<BadHttpRequestException>()).Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task Bind_MultipartOverMultipartBodyLengthLimit_ThrowsPayloadTooLarge()
    {
        var body = new MultipartBuilder().Field("data", new byte[200]).Build();

        var act = () => BindAsync(MultipartType, body, limits: new Limits(Limit, MultipartBodyLength: body.Length - 1, ValueLength: Limit));

        (await act.Should().ThrowAsync<BadHttpRequestException>()).Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Bind_DataOverValueLengthLimit_ThrowsPayloadTooLarge(bool multipart)
    {
        var body = multipart ? new MultipartBuilder().Field("data", new byte[101]).Build() : Encoding.ASCII.GetBytes("data=" + new string('a', 101));

        var act = () => BindAsync(multipart ? MultipartType : FormType, body, limits: new Limits(Limit, Limit, ValueLength: 100));

        (await act.Should().ThrowAsync<BadHttpRequestException>()).Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task Bind_MissingLimitMetadata_ThrowsInvalidOperation()
    {
        // 한도는 액션 특성(RequestSizeLimit · RequestFormLimits) 한 곳에서 온다. 특성 없는 액션에 붙이면 설정 오류다.
        var act = () => BindAsync("text/csv", CsvRow, limits: null);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---- 실패: 잘못된 전송 형식 400(ModelState) · 지원하지 않는 형식 415 ----

    public static TheoryData<string, byte[]> MalformedForms() => new()
    {
        { "multipart/form-data", new MultipartBuilder().Field("data", CsvRow).Build() },
        { "multipart/form-data; boundary=\"\"", new MultipartBuilder().Field("data", CsvRow).Build() },
        { MultipartType, Encoding.ASCII.GetBytes($"--{Boundary}\r\nContent-Disposition: form-data; name=\"data\"\r\n\r\nabc") },
        { MultipartType, Encoding.ASCII.GetBytes("no boundary line at all") },
        { MultipartType, new MultipartBuilder().Field("data", CsvRow).Field("data", CsvRow).Build() },
        { MultipartType, new MultipartBuilder().File("file", "a.csv", "text/csv", CsvRow).File("file", "b.csv", "text/csv", CsvRow).Build() },
        { FormType, Encoding.ASCII.GetBytes("data=a&data=b") },
        { FormType, Encoding.ASCII.GetBytes("data=a&DATA=b") },
    };

    [Theory]
    [MemberData(nameof(MalformedForms))]
    public async Task Bind_MalformedForm_AddsRequestLevelModelErrorWithoutException(string contentType, byte[] body)
    {
        var context = await BindAsync(contentType, body);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState.IsValid.Should().BeFalse();
        context.ModelState.Keys.Should().Equal(string.Empty);
        context.ModelState[string.Empty]!.Errors.Should().ContainSingle().Which.Exception.Should().BeNull("메시지 · 예외 원문을 응답에 싣지 않는다(1001 고정 문구)");
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("multipart/mixed; boundary=x")]
    [InlineData("not a media type")]
    public async Task Bind_UnsupportedContentType_AddsUnsupportedContentTypeError(string contentType)
    {
        var context = await BindAsync(contentType, CsvRow);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState[string.Empty]!.Errors.Should().ContainSingle().Which.Exception.Should().BeOfType<UnsupportedContentTypeException>();
    }

    // ---- 엣지: 경계값 · 빈 입력 · 바이트 보존 ----

    [Fact]
    public async Task Bind_RawBodyExactlyAtLimit_Succeeds()
    {
        var content = Enumerable.Repeat((byte)'a', Limit).ToArray();

        var payload = await BindSuccessAsync("text/csv", content, contentLength: Limit);

        payload.Content.Length.Should().Be(Limit);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Bind_DataExactlyAtValueLengthLimit_Succeeds(bool multipart)
    {
        var body = multipart ? new MultipartBuilder().Field("data", new byte[100]).Build() : Encoding.ASCII.GetBytes("data=" + new string('a', 100));

        var payload = await BindSuccessAsync(multipart ? MultipartType : FormType, body, limits: new Limits(Limit, Limit, ValueLength: 100));

        payload.Content.Length.Should().Be(100);
    }

    public static TheoryData<string?, byte[], EmployeeImportSources> EmptyInputs() => new()
    {
        { MultipartType, new MultipartBuilder().Field("note", CsvRow).Build(), EmployeeImportSources.None },
        { MultipartType, new MultipartBuilder().Build(), EmployeeImportSources.None },
        { FormType, Encoding.UTF8.GetBytes("김이름,kim@gmail.com,010-0000-0000,2000-01-01"), EmployeeImportSources.None },
        { FormType, [], EmployeeImportSources.None },
        { FormType, Encoding.ASCII.GetBytes("data="), EmployeeImportSources.Data },
        { FormType, Encoding.ASCII.GetBytes("data"), EmployeeImportSources.Data },
        { MultipartType, new MultipartBuilder().File("file", "a.csv", "text/csv", []).Build(), EmployeeImportSources.File },
        { "text/csv", [], EmployeeImportSources.Body },
        { "application/json", [0xEF, 0xBB, 0xBF, 0x20, 0x0A], EmployeeImportSources.Body },
        { null, [], EmployeeImportSources.Body },
    };

    [Theory]
    [MemberData(nameof(EmptyInputs))]
    public async Task Bind_EmptyInput_ReturnsUnknownFormatWithFoundSources(string? contentType, byte[] body, EmployeeImportSources expectedSources)
    {
        // 빈 입력 판정(21028)은 Validator 몫이다. 바인더는 찾은 출처만 켜고 형식은 정하지 않는다(T04 인계).
        var payload = await BindSuccessAsync(contentType, body);

        payload.Sources.Should().Be(expectedSources);
        payload.Format.Should().Be(EmployeeImportFormat.Unknown);
    }

    [Fact]
    public async Task Bind_FileFieldWithoutFileName_IsNotFileSource()
    {
        // 프레임워크 폼과 같게 파일 disposition(filename)이 있어야 파일 필드다. filename 있는 data는 텍스트 필드가 아니다.
        var body = new MultipartBuilder().Field("file", CsvRow).File("data", "a.csv", "text/csv", CsvRow).Build();

        var payload = await BindSuccessAsync(MultipartType, body);

        payload.Sources.Should().Be(EmployeeImportSources.None);
    }

    [Fact]
    public async Task Bind_FileNameStar_IsUsedForExtension()
    {
        var body = new MultipartBuilder().RawPart(
            "Content-Disposition: form-data; name=\"file\"; filename=\"x.bin\"; filename*=UTF-8''%EC%A7%81%EC%9B%90.json", JsonArray).Build();

        var payload = await BindSuccessAsync(MultipartType, body);

        payload.Format.Should().Be(EmployeeImportFormat.Json);
    }

    [Theory]
    [InlineData("file")]
    [InlineData("data")]
    [InlineData("raw")]
    [InlineData("urlencoded")]
    public async Task Bind_InvalidUtf8Bytes_ArePreservedWithoutReplacement(string path)
    {
        // S06-T05 ④: 프레임워크 폼 해독은 FF FE를 UTF-16 BOM으로 보거나(multipart) U+FFFD로 바꾸거나(charset 지정) %XX를 그대로 둔다(urlencoded).
        // 바인더는 원래 바이트를 넘겨 21022 판정을 Application 해독 단계 한 곳에 둔다.
        var (contentType, body) = path switch
        {
            "file" => (MultipartType, new MultipartBuilder().File("file", "a.csv", "text/csv", InvalidUtf8).Build()),
            "data" => (MultipartType, new MultipartBuilder().Field("data", InvalidUtf8, "text/plain; charset=utf-8").Build()),
            "raw" => ("text/csv", InvalidUtf8),
            _ => (FormType, Encoding.ASCII.GetBytes("data=%FF%FEa%C8%AB%ED%A0%80%E4%B8")),
        };

        var payload = await BindSuccessAsync(contentType, body);

        payload.Content.ToArray().Should().Equal(InvalidUtf8);
    }

    [Fact]
    public async Task Bind_Bom_IsPreservedInContent()
    {
        byte[] content = [0xEF, 0xBB, 0xBF, .. CsvRow];
        var body = new MultipartBuilder().Field("data", content).Build();

        var payload = await BindSuccessAsync(MultipartType, body);

        payload.Content.ToArray().Should().Equal(content);
        payload.Format.Should().Be(EmployeeImportFormat.Csv);
    }

    [Fact]
    public async Task Bind_ChunkedRawBodyWithoutContentLengthOverLimit_ThrowsPayloadTooLarge()
    {
        using var body = new MemoryStream(new byte[Limit + 1]);

        var act = () => BindAsync("text/csv", body, contentLength: null);

        (await act.Should().ThrowAsync<BadHttpRequestException>()).Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task Bind_NullContext_Throws()
    {
        var act = () => _binder.BindModelAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private async Task<EmployeeImportPayload> BindSuccessAsync(string? contentType, byte[] body, long? contentLength = -1, Limits? limits = null)
    {
        var context = await BindAsync(contentType, body, contentLength, limits ?? Limits.Default);

        context.ModelState.IsValid.Should().BeTrue();
        context.Result.IsModelSet.Should().BeTrue();
        return context.Result.Model.Should().BeOfType<EmployeeImportPayload>().Subject;
    }

    private Task<ModelBindingContext> BindAsync(string? contentType, byte[] body, long? contentLength = -1) =>
        BindAsync(contentType, body, contentLength, Limits.Default);

    private Task<ModelBindingContext> BindAsync(string? contentType, byte[] body, Limits? limits) =>
        BindAsync(contentType, body, -1, limits);

    private async Task<ModelBindingContext> BindAsync(string? contentType, byte[] body, long? contentLength, Limits? limits)
    {
        using var stream = new MemoryStream(body);
        return await BindAsync(contentType, stream, contentLength == -1 ? body.Length : contentLength, limits);
    }

    private Task<ModelBindingContext> BindAsync(string? contentType, Stream body, long? contentLength) =>
        BindAsync(contentType, body, contentLength, Limits.Default);

    private async Task<ModelBindingContext> BindAsync(string? contentType, Stream body, long? contentLength, Limits? limits)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.ContentType = contentType;
        httpContext.Request.ContentLength = contentLength;
        httpContext.Request.Body = body;
        if (limits is not null)
        {
            httpContext.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(
                    new RequestSizeLimitAttribute(limits.RequestSize),
                    new RequestFormLimitsAttribute { MultipartBodyLengthLimit = limits.MultipartBodyLength, ValueLengthLimit = limits.ValueLength }),
                "RegisterEmployees"));
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var bindingContext = new DefaultModelBindingContext
        {
            ActionContext = actionContext,
            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(EmployeeImportPayload)),
            ModelName = "payload",
            ModelState = actionContext.ModelState,
            ValueProvider = new CompositeValueProvider(),
        };

        await _binder.BindModelAsync(bindingContext);
        return bindingContext;
    }

    private sealed record Limits(long RequestSize, long MultipartBodyLength, int ValueLength)
    {
        public static Limits Default { get; } = new(Limit, Limit, Limit);
    }
}
