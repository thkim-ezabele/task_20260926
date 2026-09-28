namespace EmergencyHub.Employee.IntegrationTests.TestData;

/// <summary>
/// 과제 원문 예시(PRD-002 "원문")를 옮긴 fixture 파일입니다(S06-T06 인수 시나리오). 파일은 <c>TestData/Examples/</c>에 있고 출력 폴더로 복사됩니다.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="Csv"/>: 첫 행은 원문 <c>csv ex)</c> 그대로(<c>김이름,kim@gmail.com,010-0000-0000,2000-01-01</c>), 원문의 <c>.....</c> 자리에 같은 모양 2행, 3행.</description></item>
/// <item><description><see cref="Json"/>: 원문 <c>json ex)</c> 모양 그대로(대괄호 없는 객체 나열, 키 뒤 공백 배치 유지)이고 원문의 <c>...</c> 값만 채운 2행.</description></item>
/// </list>
/// 두 파일의 이메일은 서로 겹치지 않아 한 테스트에서 둘 다 등록할 수 있습니다. UTF-8(BOM 없음), 줄 끝 LF(<c>.gitattributes</c>)입니다.
/// </remarks>
public static class ExampleFiles
{
    /// <summary>CSV 원문 예시 파일 이름입니다(multipart <c>file</c>의 파일 이름으로도 씀).</summary>
    public const string CsvFileName = "original-example.csv";

    /// <summary>JSON 원문 예시 파일 이름입니다.</summary>
    public const string JsonFileName = "original-example.json";

    /// <summary>CSV 원문 예시의 첫 행(과제 원문 그대로)입니다.</summary>
    public const string OriginalCsvRow = "김이름,kim@gmail.com,010-0000-0000,2000-01-01";

    /// <summary>CSV 원문 예시 행 수입니다.</summary>
    public const int CsvRowCount = 3;

    /// <summary>JSON 원문 예시 행 수입니다.</summary>
    public const int JsonRowCount = 2;

    /// <summary>예시 파일 폴더(출력 폴더 안)입니다.</summary>
    public static string Directory => Path.Combine(AppContext.BaseDirectory, "TestData", "Examples");

    /// <summary>CSV 원문 예시 바이트입니다.</summary>
    public static byte[] Csv => Read(CsvFileName);

    /// <summary>JSON 원문 예시 바이트입니다.</summary>
    public static byte[] Json => Read(JsonFileName);

    /// <summary>예시 파일을 바이트 그대로 읽습니다.</summary>
    /// <param name="fileName">파일 이름(<see cref="CsvFileName"/> · <see cref="JsonFileName"/>).</param>
    /// <returns>파일 바이트.</returns>
    /// <exception cref="FileNotFoundException">파일이 없는 경우.</exception>
    public static byte[] Read(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return File.ReadAllBytes(Path.Combine(Directory, fileName));
    }
}
