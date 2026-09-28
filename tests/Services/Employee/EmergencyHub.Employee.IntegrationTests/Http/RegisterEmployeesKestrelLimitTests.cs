using System.Net;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S06-T06 완료 조건 ③(PRD-002 NFR-01 "TestServer와 Kestrel의 MaxRequestBodySize 차이를 통합 테스트로 확인", FR-05 입력 경로 표 1 MiB 초과 행):
// 테스트 안에서 루프백 임의 포트로 띄운 실제 Kestrel 호스트(fixture DB 공유)에 소켓으로 보낸다. TestServer는 Kestrel MaxRequestBodySize를 적용하지 않으므로
// TestServer 413 테스트(StatusCodePagesPipelineTests …OnTestServerRequestFormLimitsPathOnly…)는 바인더의 RequestFormLimits · RequestSizeLimit 메타데이터 경로만 본다.
// 판정: 클라이언트 결과(413 응답 또는 업로드 중 연결 끊김)는 실행마다 다르므로 서버 요청 완료 로그의 상태 코드(413)와 DB 0건으로 판정하고,
// 응답을 받은 경우에만 본문 ProblemDetails(1004)를 확인한다. 두 호스트가 Logs를 공유하므로 요청 전에 Clear한다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-05")]
[Trait("FR", "PRD-002/FR-10")]
[Trait("NFR", "PRD-002/NFR-01")]
public sealed class RegisterEmployeesKestrelLimitTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const int OneMebibyte = 1024 * 1024;

    // ---- 성공: 한도와 같은 크기(1 MiB)는 Kestrel에서도 받는다 ----

    [Fact]
    public async Task Post_KestrelRawCsvExactlyOneMebibyte_Returns201AndStores1000Rows()
    {
        var csv = PaddedCsv(OneMebibyte);
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { UseKestrel = true });
        using var client = factory.CreateKestrelClient();
        factory.Logs.Clear();
        using var request = new HttpRequestMessage(HttpMethod.Post, ImportContent.RegisterUri) { Content = ImportContent.Raw(csv, "text/csv") };

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        factory.Logs.RequestStatusCodes(ImportContent.RegisterPath).Should().Equal([201]);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(1000);
    }

    // ---- 실패: 네 입력 경로 모두 1 MiB + 1 → 서버 413 · 1004, 0건 저장 ----

    [Theory]
    [InlineData("multipart-file")]
    [InlineData("multipart-data")]
    [InlineData("form-data")]
    [InlineData("raw")]
    public async Task Post_KestrelBodyOverOneMebibyte_ServerResponds413With1004AndStoresNothing(string path)
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { UseKestrel = true });
        using var client = factory.CreateKestrelClient();
        factory.Logs.Clear();
        using var request = new HttpRequestMessage(HttpMethod.Post, ImportContent.RegisterUri)
        {
            Content = ImportContent.For(path, PaddedCsv(OneMebibyte + 1), json: false),
        };

        var outcome = await KestrelSend.SendAsync(client, request, CancellationToken);

        await ShouldBe413Async(factory, outcome);
    }

    // ---- 엣지: Content-Length 없는(chunked) 본문도 읽는 도중 한도를 넘으면 413 ----

    [Fact]
    public async Task Post_KestrelChunkedRawBodyOverOneMebibyte_ServerResponds413With1004AndStoresNothing()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { UseKestrel = true });
        using var client = factory.CreateKestrelClient();
        factory.Logs.Clear();
        using var content = new UnknownLengthContent(PaddedCsv(OneMebibyte + 1));
        content.Headers.TryAddWithoutValidation("Content-Type", "text/csv");
        using var request = new HttpRequestMessage(HttpMethod.Post, ImportContent.RegisterUri) { Content = content };

        var outcome = await KestrelSend.SendAsync(client, request, CancellationToken);

        request.Headers.TransferEncodingChunked.Should().BeTrue("길이를 모르는 본문은 chunked로 간다");
        await ShouldBe413Async(factory, outcome);
    }

    // 1,000행 CSV의 마지막 필드 뒤에 공백을 채워 정확히 size 바이트로 만든다(CSV는 전 필드 trim, 행 수는 1,000 그대로).
    private static byte[] PaddedCsv(int size)
    {
        var csv = EmployeeImportData.Csv(1000);
        var padding = size - csv.Length;
        padding.Should().BePositive();
        return [.. csv.AsSpan(0, csv.Length - 1), .. Enumerable.Repeat((byte)' ', padding), (byte)'\n'];
    }

    private async Task ShouldBe413Async(EmployeeApiFactory factory, KestrelSendOutcome outcome)
    {
        using var response = outcome.Response;
        (response is not null || outcome.ConnectionError is not null).Should().BeTrue();
        if (response is not null)
        {
            await response.ShouldBeProblemAsync(
                HttpStatusCode.RequestEntityTooLarge, 1004, CommonErrors.PayloadTooLarge.Message, ImportContent.RegisterPath, CancellationToken);
        }

        factory.Logs.RequestStatusCodes(ImportContent.RegisterPath).Should().Equal([413], "서버 쪽 최종 상태는 요청 완료 로그로 판정한다");
        factory.Logs.Events.Where(logEvent => logEvent.EventId() == 302).Should().ContainSingle("BadHttpRequestException(413) → 1004 변환 로그")
            .Which.Properties["ErrorCode"].ToString().Should().Be("1004");
        factory.Logs.Events.UnexpectedErrors().Should().BeEmpty("413은 예상한 실패라 Error가 아니다");
        factory.Logs.Events.Should().NotContain(
            logEvent => logEvent.SourceContext() == StatusCodePagesPipelineTests.RequestSizeLimitFilter,
            "Kestrel은 [RequestSizeLimit]을 IHttpMaxRequestBodySizeFeature로 서버 한도에 적용한다(TestServer와 다름)");
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    private sealed class UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            // 작은 조각으로 나눠 써서 서버가 읽는 도중 한도를 넘게 한다.
            for (var offset = 0; offset < bytes.Length; offset += 64 * 1024)
            {
                await stream.WriteAsync(bytes.AsMemory(offset, Math.Min(64 * 1024, bytes.Length - offset)), cancellationToken);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
