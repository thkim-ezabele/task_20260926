using System.Text.RegularExpressions;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// AppHost <c>EmployeeDatabaseSettings.cs</c> 원본에서 <c>const string</c> 값을 읽어 fixture 값과 대조하게 합니다(S03-T06 "생성 스크립트 grep 대조").
/// </summary>
/// <remarks>
/// AppHost 프로젝트를 참조하면 Aspire 호스팅 패키지가 테스트로 들어오므로 원본 파일을 읽습니다.
/// 생성 스크립트의 보간(<c>{DatabaseName}</c> · <c>{AppRoleName}</c>)은 같은 파일의 상수로 풉니다. 모르는 이름이 남으면 실패입니다.
/// </remarks>
public static partial class AppHostDatabaseSettingsSource
{
    /// <summary>원본 파일을 읽어 해석합니다.</summary>
    /// <returns>AppHost 구성 값.</returns>
    /// <exception cref="InvalidOperationException">필요한 상수가 없거나 두 번 이상 있거나, 보간을 풀 수 없는 경우.</exception>
    public static AppHostDatabaseSettings Read() => Parse(File.ReadAllText(RepositoryFiles.AppHostDatabaseSettingsFile));

    /// <summary>원본 텍스트를 해석합니다.</summary>
    /// <param name="source">C# 원본 텍스트.</param>
    /// <returns>AppHost 구성 값.</returns>
    /// <exception cref="InvalidOperationException">필요한 상수가 없거나 두 번 이상 있거나, 보간을 풀 수 없는 경우.</exception>
    public static AppHostDatabaseSettings Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var constants = new Dictionary<string, Match>(StringComparer.Ordinal);
        foreach (Match match in ConstantPattern().Matches(source))
        {
            var name = match.Groups["name"].Value;
            if (!constants.TryAdd(name, match))
            {
                throw new InvalidOperationException($"AppHost 상수 '{name}'가 두 번 이상 있습니다.");
            }
        }

        return new AppHostDatabaseSettings(
            Literal(constants, nameof(EmployeeDatabaseSettings.DatabaseName)),
            Literal(constants, nameof(EmployeeDatabaseSettings.AppRoleName)),
            Interpolated(constants, nameof(EmployeeDatabaseSettings.CreationScript)),
            Literal(constants, nameof(EmployeeDatabaseSettings.AppRolePasswordVariable)));
    }

    private static Match Required(Dictionary<string, Match> constants, string name) =>
        constants.TryGetValue(name, out var match)
            ? match
            : throw new InvalidOperationException($"AppHost 상수 '{name}'를 찾지 못했습니다.");

    private static string Literal(Dictionary<string, Match> constants, string name)
    {
        var match = Required(constants, name);
        return match.Groups["interpolated"].Success
            ? throw new InvalidOperationException($"AppHost 상수 '{name}'는 보간 없는 문자열이어야 합니다.")
            : match.Groups["value"].Value;
    }

    private static string Interpolated(Dictionary<string, Match> constants, string name) =>
        PlaceholderPattern().Replace(
            Required(constants, name).Groups["value"].Value,
            placeholder => Literal(constants, placeholder.Groups["name"].Value));

    [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*(?<interpolated>\$)?"(?<value>[^"]*)"\s*;""")]
    private static partial Regex ConstantPattern();

    [GeneratedRegex(@"\{(?<name>\w+)\}")]
    private static partial Regex PlaceholderPattern();
}
