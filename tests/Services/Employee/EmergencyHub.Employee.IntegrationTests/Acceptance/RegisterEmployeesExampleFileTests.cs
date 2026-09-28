using System.Net;
using System.Text;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.Http;
using EmergencyHub.Employee.IntegrationTests.TestData;

namespace EmergencyHub.Employee.IntegrationTests.Acceptance;

// S06-T06 완료 조건 ④(PRD-002 FR-10 "원문 예시를 fixture 파일로 둔 인수 시나리오", 원문 "[필수] csv · json 업로드 또는 body에 직접 입력시 작동",
// "POST /api/employee response 201", "<input type=file> 업로드 · <textarea> 직접 입력"): 과제 원문 예시 파일(TestData/Examples)을 바이트 그대로 보낸다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-05")]
[Trait("FR", "PRD-002/FR-06")]
[Trait("FR", "PRD-002/FR-10")]
public sealed class RegisterEmployeesExampleFileTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private static readonly string[] CsvEmails = ["kim@gmail.com", "lee@gmail.com", "park@gmail.com"];
    private static readonly string[] JsonEmails = ["choi@gmail.com", "jung@gmail.com"];

    // ---- 성공: 원문 예시 CSV · JSON 파일을 파일 업로드 · 텍스트 입력 · raw body로 보내면 201 ----

    public static TheoryData<string, bool> ExampleInputs()
    {
        var data = new TheoryData<string, bool>();
        foreach (var path in ImportContent.Paths)
        {
            data.Add(path, false);
            data.Add(path, true);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ExampleInputs))]
    public async Task Post_OriginalExampleFile_Returns201AndStoresEveryRow(string path, bool json)
    {
        var bytes = json ? ExampleFiles.Json : ExampleFiles.Csv;
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = path == "multipart-file"
            ? ImportContent.MultipartFile(bytes, json ? ExampleFiles.JsonFileName : ExampleFiles.CsvFileName)
            : ImportContent.For(path, bytes, json);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "원문: POST /api/employee response 201 (created)");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        body.RootElement.GetProperty("count").GetInt32().Should().Be(json ? ExampleFiles.JsonRowCount : ExampleFiles.CsvRowCount);
        (await EmployeeRows.ListAsync(Database, CancellationToken)).Select(row => row.NormalizedEmail)
            .Should().Equal(json ? JsonEmails : CsvEmails, "행은 입력 순서(UUID v7)로 저장된다");
    }

    // 브라우저 폼 한 번에 CSV · JSON 파일을 차례로 올린 시나리오: 두 파일 모두 저장되고 같은 CSV를 다시 올리면 409 + 충돌 행 번호, 저장 행은 그대로.
    [Fact]
    public async Task Post_CsvThenJsonThenCsvAgain_Stores5RowsAndRejectsReuploadWith409RowNumbers()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using (var csv = ImportContent.MultipartFile(ExampleFiles.Csv, ExampleFiles.CsvFileName, "text/csv"))
        {
            (await client.PostAsync(ImportContent.RegisterUri, csv, CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using (var json = ImportContent.MultipartFile(ExampleFiles.Json, ExampleFiles.JsonFileName, "application/json"))
        {
            (await client.PostAsync(ImportContent.RegisterUri, json, CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var again = ImportContent.MultipartFile(ExampleFiles.Csv, ExampleFiles.CsvFileName, "text/csv");

        using var response = await client.PostAsync(ImportContent.RegisterUri, again, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.Conflict, 23001, "이미 등록된 이메일입니다.", ImportContent.RegisterPath, CancellationToken, hasErrors: true);
        problem.FieldCodes().Should().Equal([("rows[1].email", 23001), ("rows[2].email", 23001), ("rows[3].email", 23001)]);
        (await EmployeeRows.ListAsync(Database, CancellationToken)).Select(row => row.NormalizedEmail).Should().Equal([.. CsvEmails, .. JsonEmails]);
    }

    // ---- 실패: 원문의 "위 필드들은 필수값임" — 예시 행에서 필드 하나를 비우면 400 · 해당 행 필수 코드, 0건 ----

    [Theory]
    [InlineData(0, "rows[2].name", 21007)]
    [InlineData(1, "rows[2].email", 21003)]
    [InlineData(2, "rows[2].tel", 21010)]
    [InlineData(3, "rows[2].joined", 21015)]
    public async Task Post_ExampleCsvWithRequiredFieldEmptied_Returns400WithRequiredCodeAndStoresNothing(int field, string key, int code)
    {
        var lines = Encoding.UTF8.GetString(ExampleFiles.Csv).Split('\n');
        var cells = lines[1].Split(',');
        cells[field] = "  ";
        lines[1] = string.Join(',', cells);
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.MultipartFile(Encoding.UTF8.GetBytes(string.Join('\n', lines)), ExampleFiles.CsvFileName);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.BadRequest, 1001, CommonErrors.ValidationFailed.Message, ImportContent.RegisterPath, CancellationToken);
        problem.FieldCodes().Should().Equal([(key, code)]);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(0);
    }

    // ---- 엣지: 예시 JSON 파일의 확장자만으로 형식 판별(파트 Content-Type 없음) ----

    [Fact]
    public async Task Post_ExampleJsonFileWithoutPartContentType_DetectsJsonByExtensionAndReturns201()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.MultipartFile(ExampleFiles.Json, ExampleFiles.JsonFileName, contentType: null);

        using var response = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await EmployeeRows.CountAsync(Database, CancellationToken)).Should().Be(ExampleFiles.JsonRowCount);
    }
}
