using System.Text;
using EmergencyHub.Employee.Api.Employees.Import;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;

namespace EmergencyHub.Employee.Api.UnitTests.Employees.Import;

// S06-T05 형식 판별(ADR-0026 2절, PRD-002 FR-05): Content-Type → 파일 확장자 → 내용 추정 순서.
// 1단계는 text/csv · application/json만(매개변수 무시), 2단계는 .csv · .json(대소문자 무시), 3단계는 BOM과 앞 공백(0x20 · 0x09 · 0x0D · 0x0A)을
// 건너뛴 첫 바이트가 [ · {이면 Json, 그 밖이면 Csv. 입력이 비었으면(Validator의 빈 입력 정의와 같음) 형식을 정하지 않는다(Unknown 0).
[Trait("FR", "PRD-002/FR-05")]
public sealed class EmployeeImportFormatDetectorTests
{
    private static readonly byte[] CsvRow = Encoding.UTF8.GetBytes("홍길동,hong@example.com,010-1234-5678,2020-01-02");
    private static readonly byte[] JsonArray = Encoding.UTF8.GetBytes("[{\"name\":\"홍길동\"}]");

    // ---- 성공: 단계별 판별 ----

    [Theory]
    [InlineData("text/csv", EmployeeImportFormat.Csv)]
    [InlineData("application/json", EmployeeImportFormat.Json)]
    [InlineData("text/csv; charset=utf-8", EmployeeImportFormat.Csv)]
    [InlineData("application/json; charset=utf-16", EmployeeImportFormat.Json)]
    [InlineData("TEXT/CSV", EmployeeImportFormat.Csv)]
    [InlineData("Application/Json", EmployeeImportFormat.Json)]
    public void FromMediaType_SupportedType_ReturnsFormatIgnoringParametersAndCase(string contentType, EmployeeImportFormat expected) =>
        EmployeeImportFormatDetector.FromMediaType(contentType).Should().Be(expected);

    [Theory]
    [InlineData("employees.csv", EmployeeImportFormat.Csv)]
    [InlineData("employees.json", EmployeeImportFormat.Json)]
    [InlineData("EMPLOYEES.CSV", EmployeeImportFormat.Csv)]
    [InlineData("직원.Json", EmployeeImportFormat.Json)]
    [InlineData("a.b.csv", EmployeeImportFormat.Csv)]
    public void FromFileName_KnownExtension_ReturnsFormatIgnoringCase(string fileName, EmployeeImportFormat expected) =>
        EmployeeImportFormatDetector.FromFileName(fileName).Should().Be(expected);

    [Theory]
    [InlineData("[{}]", EmployeeImportFormat.Json)]
    [InlineData("{\"name\":\"a\"}", EmployeeImportFormat.Json)]
    [InlineData("{},{}", EmployeeImportFormat.Json)]
    [InlineData("홍길동,hong@example.com,010-1234-5678,2020-01-02", EmployeeImportFormat.Csv)]
    [InlineData("\"[a]\",b,c,d", EmployeeImportFormat.Csv)]
    public void FromContent_FirstCharacter_ReturnsJsonForBracketOrBraceElseCsv(string content, EmployeeImportFormat expected) =>
        EmployeeImportFormatDetector.FromContent(Encoding.UTF8.GetBytes(content)).Should().Be(expected);

    [Fact]
    public void Detect_ContentTypeWins_OverExtensionAndContent()
    {
        EmployeeImportFormatDetector.Detect("text/csv", "employees.json", JsonArray).Should().Be(EmployeeImportFormat.Csv);
        EmployeeImportFormatDetector.Detect("application/json", "employees.csv", CsvRow).Should().Be(EmployeeImportFormat.Json);
    }

    [Fact]
    public void Detect_UnknownContentType_FallsBackToExtensionBeforeContent()
    {
        EmployeeImportFormatDetector.Detect("application/octet-stream", "employees.csv", JsonArray).Should().Be(EmployeeImportFormat.Csv);
        EmployeeImportFormatDetector.Detect("text/plain", "employees.json", CsvRow).Should().Be(EmployeeImportFormat.Json);
    }

    [Fact]
    public void Detect_NoContentTypeAndUnknownExtension_FallsBackToContent()
    {
        EmployeeImportFormatDetector.Detect(null, "employees.txt", JsonArray).Should().Be(EmployeeImportFormat.Json);
        EmployeeImportFormatDetector.Detect(null, null, CsvRow).Should().Be(EmployeeImportFormat.Csv);
    }

    // ---- 실패: 판별 단계에서 형식을 정하지 못함 ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("text/plain")]
    [InlineData("multipart/form-data; boundary=x")]
    [InlineData("application/x-www-form-urlencoded")]
    [InlineData("application/problem+json")]
    [InlineData("text/csvx")]
    [InlineData("not a media type")]
    public void FromMediaType_OtherOrMissingOrInvalid_ReturnsUnknown(string? contentType) =>
        EmployeeImportFormatDetector.FromMediaType(contentType).Should().Be(EmployeeImportFormat.Unknown);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("employees")]
    [InlineData("employees.txt")]
    [InlineData("employees.csv.txt")]
    [InlineData("employees.")]
    [InlineData(".csvx")]
    public void FromFileName_OtherOrMissingExtension_ReturnsUnknown(string? fileName) =>
        EmployeeImportFormatDetector.FromFileName(fileName).Should().Be(EmployeeImportFormat.Unknown);

    // ---- 엣지: BOM · 앞 공백, 빈 입력 ----

    [Theory]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x5B, 0x5D })]
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x7B, 0x7D })]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x0A, 0x20, 0x5B })]
    public void FromContent_BomAndLeadingBlanks_AreSkippedBeforeFirstCharacter(byte[] content) =>
        EmployeeImportFormatDetector.FromContent(content).Should().Be(EmployeeImportFormat.Json);

    [Theory]
    [InlineData(new byte[] { 0x0B, 0x5B })]
    [InlineData(new byte[] { 0xC2, 0xA0, 0x7B })]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0xEF, 0xBB, 0xBF, 0x5B })]
    [InlineData(new byte[] { 0x20, 0xEF, 0xBB, 0xBF, 0x5B })]
    public void FromContent_WhitespaceLikeOutsideBlankSetOrSecondBom_IsCsv(byte[] content) =>
        EmployeeImportFormatDetector.FromContent(content).Should().Be(EmployeeImportFormat.Csv);

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF })]
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A })]
    public void FromContent_Blank_ReturnsUnknown(byte[] content) =>
        EmployeeImportFormatDetector.FromContent(content).Should().Be(EmployeeImportFormat.Unknown);

    [Theory]
    [InlineData("text/csv", "employees.csv")]
    [InlineData("application/json", null)]
    [InlineData(null, "employees.json")]
    public void Detect_BlankContent_ReturnsUnknownEvenWithContentTypeOrExtension(string? contentType, string? fileName)
    {
        // ADR-0026 2절: 입력이 비어 있으면 형식을 정하지 않는다. Validator가 빈 입력(21028)을 먼저 보고한다.
        EmployeeImportFormatDetector.Detect(contentType, fileName, [0xEF, 0xBB, 0xBF, 0x20]).Should().Be(EmployeeImportFormat.Unknown);
    }
}
