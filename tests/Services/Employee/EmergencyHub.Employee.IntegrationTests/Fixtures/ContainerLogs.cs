using DotNet.Testcontainers.Containers;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// 컨테이너 로그를 파일로 남깁니다. 환경 변수 <see cref="DirectoryVariable"/>가 있을 때만 씁니다(CI는 실패 시 이 폴더를 아티팩트로 올림).
/// </summary>
/// <remarks>
/// Testcontainers는 폐기할 때 컨테이너를 지우므로 실패 뒤 <c>docker logs</c>로 볼 수 없습니다. 그래서 폐기 직전과 시작 실패 때 fixture가 직접 받아 둡니다.
/// PostgreSQL 서버 로그에는 비밀번호가 없습니다(초기화 스크립트가 <c>log_statement = 'none'</c>, 연결 문자열은 로그에 쓰지 않음).
/// </remarks>
public static class ContainerLogs
{
    /// <summary>로그 폴더 환경 변수입니다. 비어 있으면 저장하지 않습니다(로컬 기본).</summary>
    public const string DirectoryVariable = "EMERGENCYHUB_CONTAINER_LOG_DIRECTORY";

    /// <summary>환경 변수 <see cref="DirectoryVariable"/>의 폴더에 저장합니다(<see cref="SaveAsync(IContainer, string, string?)"/>).</summary>
    /// <param name="container">컨테이너.</param>
    /// <param name="name">파일 이름 접두사.</param>
    /// <returns>저장한 파일 경로. 저장하지 않았으면 <see langword="null"/>.</returns>
    public static Task<string?> SaveAsync(IContainer container, string name) =>
        SaveAsync(container, name, Environment.GetEnvironmentVariable(DirectoryVariable));

    /// <summary>컨테이너 표준 출력 · 오류를 <c>&lt;폴더&gt;/&lt;이름&gt;-&lt;컨테이너 ID 12자&gt;.log</c>로 저장합니다.</summary>
    /// <param name="container">컨테이너(만들어지기 전이면 저장하지 않음).</param>
    /// <param name="name">파일 이름 접두사.</param>
    /// <param name="directory">저장 폴더. 비어 있으면 저장하지 않습니다.</param>
    /// <returns>저장한 파일 경로. 저장하지 않았으면 <see langword="null"/>.</returns>
    public static async Task<string?> SaveAsync(IContainer container, string name, string? directory)
    {
        ArgumentNullException.ThrowIfNull(container);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrEmpty(container.Id))
        {
            return null;
        }

        var (stdout, stderr) = await container.GetLogsAsync(timestampsEnabled: true, ct: CancellationToken.None);

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{name}-{container.Id[..Math.Min(12, container.Id.Length)]}.log");
        await File.WriteAllTextAsync(path, $"# stdout{Environment.NewLine}{stdout}{Environment.NewLine}# stderr{Environment.NewLine}{stderr}", CancellationToken.None);
        return path;
    }
}
