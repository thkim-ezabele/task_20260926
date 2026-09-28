using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EmergencyHub.Employee.IntegrationTests.TestData;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S06-T06 도구(developer): 테스트 호스트 전용 경합 hook(이메일 사전 조회 뒤 · 커밋 전)과 고정 IIdGenerator 주입의 동작 확인.
// 동시 경합 (a) · 같은 ID 재전송 (b)의 본 테스트(행 번호 없음 · 먼저 커밋된 쪽만 남음 · 로그 202 등)는 tester가 쓴다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-10")]
public sealed class EmployeeApiFactoryHookTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string RegisterPath = "/api/employee";

    // ---- 성공 ----

    [Fact]
    public async Task AfterEmailLookup_Set_ReceivesNormalizedEmailsAndRequestStillSucceeds()
    {
        var received = new List<IReadOnlyCollection<string>>();
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            AfterEmailLookup = (emails, _) =>
            {
                received.Add([.. emails]);
                return Task.CompletedTask;
            },
        });
        using var client = factory.CreateClient();

        using var response = await PostCsvAsync(client, "김이름, Kim@Gmail.COM ,010-0000-0000,2000-01-01\n");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        received.Should().ContainSingle().Which.Should().Equal(["kim@gmail.com"], "hook은 사전 조회에 넘긴 정규화 이메일을 받는다");
        (await CountEmployeesAsync()).Should().Be(1);
    }

    [Fact]
    public async Task IdGenerator_Scripted_RegisteredEmployeesUseScriptedIdsInOrder()
    {
        var ids = ScriptedIdGenerator.WithRandomIds(2);
        var expected = new[] { ids.NewId(), ids.NewId() };
        ids.Rewind();
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { IdGenerator = ids });
        using var client = factory.CreateClient();

        using var response = await PostCsvAsync(client, EmployeeImportData.Csv(2));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        body.RootElement.GetProperty("ids").EnumerateArray().Select(id => id.GetGuid()).Should().Equal(expected);
        ids.Issued.Should().Be(2);
    }

    // ---- 실패 ----

    [Fact]
    public async Task AfterEmailLookup_HookThrows_Returns500AndStoresNothing()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            AfterEmailLookup = (_, _) => throw new InvalidOperationException("hook 실패 주입"),
        });
        using var client = factory.CreateClient();

        using var response = await PostCsvAsync(client, EmployeeImportData.Csv(1));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "hook 예외는 그대로 전파되어 전역 예외 처리기가 받는다");
        (await CountEmployeesAsync()).Should().Be(0);
    }

    [Fact]
    public async Task IdGenerator_ExhaustedDuringRequest_Returns500AndStoresNothing()
    {
        await using var factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions { IdGenerator = ScriptedIdGenerator.WithRandomIds(1) });
        using var client = factory.CreateClient();

        using var response = await PostCsvAsync(client, EmployeeImportData.Csv(2));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await CountEmployeesAsync()).Should().Be(0);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task AfterEmailLookup_CommitsConflictingRowBeforeRequestCommit_RequestGets409AndHookRowRemains()
    {
        var hookRow = new EmployeeBuilder().WithEmail("kim@gmail.com").Build();
        EmployeeApiFactory? factory = null;
        await using var created = factory = new EmployeeApiFactory(Database, new EmployeeApiFactoryOptions
        {
            AfterEmailLookup = async (_, cancellationToken) =>
                (await EmployeeCommits.AddAndCommitAsync(factory!.Services, hookRow, cancellationToken)).IsSuccess.Should().BeTrue(),
        });
        using var client = factory.CreateClient();

        using var response = await PostCsvAsync(client, "김이름,kim@gmail.com,010-0000-0000,2000-01-01\n");

        // 사전 조회는 빈 결과였고, hook이 다른 스코프 · 연결로 먼저 커밋해 요청의 INSERT가 23505(ux) → 23001을 받는다.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        body.RootElement.GetProperty("code").GetInt32().Should().Be(23001);
        (await EmployeeRows.ListAsync(Database, CancellationToken)).Should().ContainSingle()
            .Which.Id.Should().Be(hookRow.Id.Value, "hook이 커밋한 행만 남는다");
    }

    private async Task<HttpResponseMessage> PostCsvAsync(HttpClient client, string csv) => await PostCsvAsync(client, Encoding.UTF8.GetBytes(csv));

    private async Task<HttpResponseMessage> PostCsvAsync(HttpClient client, byte[] csv)
    {
        using var content = new ByteArrayContent(csv);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        return await client.PostAsync(new Uri(RegisterPath, UriKind.Relative), content, CancellationToken);
    }

    private async Task<long> CountEmployeesAsync() => await EmployeeRows.CountAsync(Database, CancellationToken);
}
