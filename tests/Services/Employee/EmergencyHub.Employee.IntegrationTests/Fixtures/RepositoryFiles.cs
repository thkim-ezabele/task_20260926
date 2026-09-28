using System.Xml.Linq;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// 저장소 파일 위치입니다. 테스트 출력 폴더에서 위로 올라가 <c>EmergencyHub.sln</c>이 있는 디렉터리를 루트로 정합니다
/// (testing-strategy.md "테스트 DB 구성 (fixture)": 초기화 스크립트는 복사본 없이 AppHost 원본을 쓴다).
/// </summary>
public static class RepositoryFiles
{
    private const string SolutionFileName = "EmergencyHub.sln";

    /// <summary>저장소 루트입니다.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>AppHost 프로젝트 폴더입니다.</summary>
    public static string AppHostDirectory => Path.Combine(Root, "src", "Aspire", "EmergencyHub.AppHost");

    /// <summary>Employee Api 프로젝트 폴더입니다(<see cref="ApiContentRoot"/>가 설정 파일을 읽음).</summary>
    public static string EmployeeApiDirectory => Path.Combine(Root, "src", "Services", "Employee", "EmergencyHub.Employee.Api");

    /// <summary>AppHost가 <c>WithInitFiles</c>로 넣는 PostgreSQL 초기화 스크립트 폴더입니다(AppHost <c>EmployeeDatabaseSettings.InitFilesDirectory</c>).</summary>
    public static string PostgresInitDirectory => Path.Combine(AppHostDirectory, "postgres-init");

    /// <summary>AppHost Employee DB 구성 값 원본 파일입니다(생성 스크립트 대조용).</summary>
    public static string AppHostDatabaseSettingsFile => Path.Combine(AppHostDirectory, "EmployeeDatabaseSettings.cs");

    /// <summary><c>Directory.Build.props</c>의 속성 값을 읽습니다. 같은 이름의 속성은 정확히 하나여야 합니다.</summary>
    /// <param name="name">속성 이름.</param>
    /// <returns>속성 값.</returns>
    /// <exception cref="InvalidOperationException">속성이 없거나 두 개 이상인 경우.</exception>
    public static string BuildProperty(string name) =>
        XDocument.Load(Path.Combine(Root, "Directory.Build.props"))
            .Descendants(name)
            .Single()
            .Value;

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"{SolutionFileName}이 있는 저장소 루트를 {AppContext.BaseDirectory} 위쪽에서 찾지 못했습니다.");
    }
}
