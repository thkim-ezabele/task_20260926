using System.Globalization;
using System.Text;

namespace EmergencyHub.Employee.IntegrationTests.TestData;

/// <summary>
/// 일괄 등록 요청 본문(CSV · JSON)을 만드는 도우미입니다(S06-T06 1,000행 성능 측정 · 1,000행 초과 · 개인정보 검사).
/// </summary>
/// <remarks>
/// 행 <c>i</c>(0부터)의 값: 이름 <c>{prefix}직원{i}</c>, 이메일 <c>{prefix}user{i}@example.com</c>(행마다 다름), 전화 <c>010-{i / 10000:0000}-{i % 10000:0000}</c>,
/// 입사일 <c>2000-01-01</c>. <paramref name="prefix"/>를 바꾸면 다른 요청과 이메일이 겹치지 않습니다. UTF-8(BOM 없음), 줄 끝 <c>\n</c>입니다.
/// </remarks>
public static class EmployeeImportData
{
    /// <summary>입사일 값(모든 행 같음)입니다.</summary>
    public const string JoinedOn = "2000-01-01";

    /// <summary>행 <paramref name="index"/>의 이름입니다.</summary>
    /// <param name="index">행 순번(0부터).</param>
    /// <param name="prefix">값 앞에 붙일 문자열(ASCII 영숫자 권장).</param>
    /// <returns>이름.</returns>
    public static string Name(int index, string prefix = "") => string.Create(CultureInfo.InvariantCulture, $"{prefix}직원{index}");

    /// <summary>행 <paramref name="index"/>의 이메일입니다(이미 소문자라 정규화 값과 같음).</summary>
    /// <param name="index">행 순번(0부터).</param>
    /// <param name="prefix">값 앞에 붙일 문자열(ASCII 소문자 · 숫자 권장).</param>
    /// <returns>이메일.</returns>
    public static string Email(int index, string prefix = "") => string.Create(CultureInfo.InvariantCulture, $"{prefix}user{index}@example.com");

    /// <summary>행 <paramref name="index"/>의 전화번호입니다(<c>010-0000-0000</c> 형식, 10,000행마다 가운데 자리 증가).</summary>
    /// <param name="index">행 순번(0부터, 99,999,999 이하).</param>
    /// <returns>전화번호.</returns>
    public static string Tel(int index) => string.Create(CultureInfo.InvariantCulture, $"010-{index / 10000:0000}-{index % 10000:0000}");

    /// <summary>헤더 없는 CSV 본문(<c>name,email,tel,joined</c>)을 만듭니다.</summary>
    /// <param name="rows">행 수(0 이상).</param>
    /// <param name="prefix">값 앞에 붙일 문자열.</param>
    /// <returns>UTF-8 바이트.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rows"/>가 음수인 경우.</exception>
    public static byte[] Csv(int rows, string prefix = "")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rows);

        var builder = new StringBuilder();
        for (var index = 0; index < rows; index++)
        {
            builder.Append(CultureInfo.InvariantCulture, $"{Name(index, prefix)},{Email(index, prefix)},{Tel(index)},{JoinedOn}\n");
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    /// <summary>JSON 배열 본문(<c>[{"name":…,"email":…,"tel":…,"joined":…}, …]</c>)을 만듭니다.</summary>
    /// <param name="rows">행 수(0 이상).</param>
    /// <param name="prefix">값 앞에 붙일 문자열.</param>
    /// <returns>UTF-8 바이트.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rows"/>가 음수인 경우.</exception>
    public static byte[] Json(int rows, string prefix = "")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rows);

        var items = Enumerable.Range(0, rows).Select(index => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = Name(index, prefix),
            ["email"] = Email(index, prefix),
            ["tel"] = Tel(index),
            ["joined"] = JoinedOn,
        });

        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(items);
    }

    /// <summary>개인정보 노출 검사에 쓸 입력 값(행마다 이름 · 이메일 · 전화번호)을 돌려줍니다.</summary>
    /// <param name="rows">행 수(0 이상).</param>
    /// <param name="prefix">값 앞에 붙일 문자열.</param>
    /// <returns>값 목록(행 순서, 행마다 이름 → 이메일 → 전화번호).</returns>
    public static IReadOnlyList<string> PersonalValues(int rows, string prefix = "")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rows);

        return [.. Enumerable.Range(0, rows).SelectMany(index => new[] { Name(index, prefix), Email(index, prefix), Tel(index) })];
    }
}
