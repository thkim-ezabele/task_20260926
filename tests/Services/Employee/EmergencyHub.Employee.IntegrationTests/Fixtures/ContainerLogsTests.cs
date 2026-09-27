using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T06 CI 실패 진단: fixture가 컨테이너 로그를 폴더에 저장하고(CI는 실패 시 container-logs 아티팩트), 폴더가 없으면 아무것도 쓰지 않는다.
// 로그에 비밀번호가 없어야 한다(초기화 스크립트 log_statement = 'none', 연결 문자열 미기록).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-09")]
public sealed class ContainerLogsTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // ---- 성공 ----

    [Fact]
    public async Task SaveContainerLogsAsync_WithDirectory_WritesServerLogWithoutPasswords()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"eh-container-logs-{Guid.NewGuid():N}");
        try
        {
            var path = await Database.SaveContainerLogsAsync(directory);

            path.Should().NotBeNull().And.StartWith(directory);
            var text = await File.ReadAllTextAsync(path!, CancellationToken);
            text.Should().Contain("database system is ready to accept connections");
            text.Should().NotContain(new NpgsqlConnectionStringBuilder(Database.WriteConnectionString).Password);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    // ---- 실패 · 엣지 ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveContainerLogsAsync_WithoutDirectory_WritesNothing(string? directory)
    {
        var path = await Database.SaveContainerLogsAsync(directory);

        path.Should().BeNull();
    }
}
