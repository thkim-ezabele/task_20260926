using System.Text;
using System.Text.Json;

namespace EmergencyHub.Employee.IntegrationTests.TestData;

// S06-T06 도구(developer): 원문 예시 fixture 파일과 요청 본문 생성기. 인수 시나리오(파일 업로드 · 텍스트 입력 201)는 tester가 쓴다.
public sealed class ExampleFilesTests
{
    private static readonly string[] RequiredKeys = ["name", "email", "tel", "joined"];
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    // ---- 성공 ----

    [Fact]
    public void Csv_OriginalExample_FirstRowIsVerbatimAndRowCountMatches()
    {
        var text = StrictUtf8.GetString(ExampleFiles.Csv);

        text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(ExampleFiles.CsvRowCount)
            .And.Subject.First().Should().Be(ExampleFiles.OriginalCsvRow);
        text.Should().NotContain("\r", "줄 끝은 LF(.gitattributes)");
    }

    [Fact]
    public void Json_OriginalExample_IsBracketlessListOfObjectsWithFourRequiredKeys()
    {
        var text = StrictUtf8.GetString(ExampleFiles.Json).Trim();

        text.Should().StartWith("{", "원문 json ex)는 대괄호 없는 객체 나열이다");
        using var document = JsonDocument.Parse("[" + text + "]");
        document.RootElement.EnumerateArray().Should().HaveCount(ExampleFiles.JsonRowCount)
            .And.OnlyContain(item => item.EnumerateObject().Select(property => property.Name).SequenceEqual(RequiredKeys));
    }

    // ---- 실패 ----

    [Fact]
    public void Read_MissingFile_ThrowsFileNotFound()
    {
        var act = () => ExampleFiles.Read("missing.csv");

        act.Should().Throw<FileNotFoundException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void Files_NoBomAndNoSharedEmails()
    {
        ExampleFiles.Csv.Take(3).Should().NotEqual(Encoding.UTF8.Preamble.ToArray());
        ExampleFiles.Json.Take(3).Should().NotEqual(Encoding.UTF8.Preamble.ToArray());

        var csvEmails = StrictUtf8.GetString(ExampleFiles.Csv).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(row => row.Split(',')[1]);
        using var json = JsonDocument.Parse("[" + StrictUtf8.GetString(ExampleFiles.Json).Trim() + "]");
        var jsonEmails = json.RootElement.EnumerateArray().Select(item => item.GetProperty("email").GetString());
        csvEmails.Should().NotIntersectWith(jsonEmails, "두 파일을 한 테스트에서 함께 등록할 수 있다");
    }

    [Fact]
    public void EmployeeImportData_ThousandRows_UnderOneMebibyteWithUniqueEmails()
    {
        var csv = EmployeeImportData.Csv(1000);
        var json = EmployeeImportData.Json(1000);

        csv.Length.Should().BeLessThan(1024 * 1024);
        json.Length.Should().BeLessThan(1024 * 1024);
        StrictUtf8.GetString(csv).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(row => row.Split(',')[1]).Should().OnlyHaveUniqueItems().And.HaveCount(1000);
        EmployeeImportData.Tel(12345).Should().Be("010-0001-2345");
        EmployeeImportData.PersonalValues(1).Should().Equal("직원0", "user0@example.com", "010-0000-0000");
    }
}
