using System.Text.Json;
using System.Text.Json.Nodes;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T07 도우미: Api 설정 사본에서 Serilog 싱크(WriteTo)만 빼고 나머지(최소 수준 · 속성)는 그대로 둔다.
[Trait("FR", "PRD-001/FR-09")]
public sealed class ApiContentRootTests : IDisposable
{
    private readonly string _source = Directory.CreateTempSubdirectory("api-content-root-src-").FullName;

    public void Dispose() => ApiContentRoot.Delete(_source);

    // ---- 성공 ----

    [Fact]
    public void Create_ApiProjectSettings_CopiesAllSettingsFilesWithoutWriteTo()
    {
        var target = ApiContentRoot.Create(RepositoryFiles.EmployeeApiDirectory);
        try
        {
            var copied = Directory.EnumerateFiles(target).Select(Path.GetFileName).ToList();
            copied.Should().BeEquivalentTo("appsettings.json", "appsettings.Development.json");

            var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(target, "appsettings.json")))!;
            settings["Serilog"]!.AsObject().ContainsKey("WriteTo").Should().BeFalse();
            settings["Serilog"]!["MinimumLevel"]!["Default"]!.GetValue<string>().Should().Be("Information");
            settings["Serilog"]!["Properties"]!["ServiceName"]!.GetValue<string>().Should().Be("employee");
        }
        finally
        {
            ApiContentRoot.Delete(target);
        }
    }

    // ---- 실패 ----

    [Fact]
    public void Create_NoBaseSettingsFile_ThrowsFileNotFound()
    {
        var act = () => ApiContentRoot.Create(_source);

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void RemoveSerilogSinks_TopLevelArray_ThrowsJsonException()
    {
        var act = () => ApiContentRoot.RemoveSerilogSinks("[1, 2]");

        act.Should().Throw<JsonException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void RemoveSerilogSinks_KeysInDifferentCase_StillRemovesWriteTo()
    {
        // 설정 키는 대소문자를 구분하지 않으므로 파일 키도 같게 본다.
        var result = ApiContentRoot.RemoveSerilogSinks("""{ "serilog": { "writeTo": [ { "Name": "Console" } ], "Using": [] } }""");

        var serilog = JsonNode.Parse(result)!["serilog"]!.AsObject();
        serilog.ContainsKey("writeTo").Should().BeFalse();
        serilog.ContainsKey("Using").Should().BeTrue();
    }

    [Fact]
    public void RemoveSerilogSinks_NoSerilogSectionWithComment_KeepsOtherSettings()
    {
        var result = ApiContentRoot.RemoveSerilogSinks("""
            {
              // 주석은 건너뛴다
              "Logging": { "LogLevel": { "Default": "Warning" } },
            }
            """);

        JsonNode.Parse(result)!["Logging"]!["LogLevel"]!["Default"]!.GetValue<string>().Should().Be("Warning");
    }

    [Fact]
    public void Delete_MissingDirectory_DoesNothing()
    {
        var missing = Path.Combine(_source, "missing");

        var act = () => ApiContentRoot.Delete(missing);

        act.Should().NotThrow();
    }
}
