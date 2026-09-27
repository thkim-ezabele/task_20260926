using System.Xml.Linq;

namespace EmergencyHub.AppHost.UnitTests;

/// <summary>저장소 파일을 읽는 도우미. 테스트 출력 폴더에서 위로 올라가 EmergencyHub.sln이 있는 디렉터리를 찾는다.</summary>
internal static class RepositoryFiles
{
    internal static string Root { get; } = FindRoot();

    internal static string AppHostDirectory => Path.Combine(Root, "src", "Aspire", "EmergencyHub.AppHost");

    /// <summary>Directory.Build.props의 속성 값(유일해야 함)을 읽는다.</summary>
    internal static string BuildProperty(string name) =>
        XDocument.Load(Path.Combine(Root, "Directory.Build.props"))
            .Descendants(name)
            .Single()
            .Value;

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EmergencyHub.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("EmergencyHub.sln이 있는 저장소 루트를 찾지 못했습니다.");
    }
}
