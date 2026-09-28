using System.Text;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Application.Employees.Import;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Import;

// 원본: PRD-002 FR-03 인수 조건, ADR-0026 3 · 4 · 6절. 헤더 없음(TD-028), 열 순서 name,email,tel,joined.
public sealed class CsvImportParserTests
{
    private const string Example = "김이름,kim@gmail.com,010-0000-0000,2000-01-01";

    // 제어 문자를 담은 테스트 데이터. 발견 단계 직렬화가 값을 바꾸지 않도록 열거를 끄고 입력 보존을 단언한다(BL-131).
    public static TheoryData<string, char> ControlCharacterFields => new()
    {
        { "a\0b", '\0' },
        { "a\u0001b", '\u0001' },
        { "a\u007Fb", '\u007F' },
        { "a\rb", '\r' },
    };

    // ---- 성공 ----

    [Fact]
    public void Parse_PrdExampleRow_ReturnsOneRow()
    {
        var parsed = ParseSuccess(Example);

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(new ImportRow(1, "김이름", "kim@gmail.com", "010-0000-0000", "2000-01-01"));
    }

    [Fact]
    public void Parse_LfAndCrLf_SplitRecordsAndNumberPhysicalLines()
    {
        var parsed = ParseSuccess("a,b,c,d\ne,f,g,h\r\ni,j,k,l\n");

        parsed.Rows.Should().Equal(
            new ImportRow(1, "a", "b", "c", "d"),
            new ImportRow(2, "e", "f", "g", "h"),
            new ImportRow(3, "i", "j", "k", "l"));
    }

    [Fact]
    public void Parse_LastLineWithoutNewline_IsParsed()
    {
        var parsed = ParseSuccess("a,b,c,d\r\ne,f,g,h");

        parsed.Rows.Select(row => row.RowNumber).Should().Equal(1, 2);
        parsed.Rows[1].Should().Be(new ImportRow(2, "e", "f", "g", "h"));
    }

    [Fact]
    public void Parse_BlankLines_AreIgnoredButCounted()
    {
        // 행 번호는 물리 줄 번호(빈 줄 포함, 1부터).
        var parsed = ParseSuccess("\na,b,c,d\n\n\r\ne,f,g,h\n\n");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Select(row => row.RowNumber).Should().Equal(2, 5);
    }

    [Fact]
    public void Parse_AllFields_AreTrimmed()
    {
        var parsed = ParseSuccess(" 김이름 ,\tkim@gmail.com\t,  010-0000-0000,2000-01-01  ");

        parsed.Rows.Should().Equal(new ImportRow(1, "김이름", "kim@gmail.com", "010-0000-0000", "2000-01-01"));
    }

    [Fact]
    public void Parse_QuotedFields_AreTrimmedInsideAndOutsideQuotes()
    {
        var parsed = ParseSuccess("  \" 김, 이름 \"  ,\"kim@gmail.com\",\"010-0000-0000\" ,2000-01-01");

        parsed.Rows.Should().Equal(new ImportRow(1, "김, 이름", "kim@gmail.com", "010-0000-0000", "2000-01-01"));
    }

    [Fact]
    public void Parse_CommaInsideQuotes_IsFieldCharacter()
    {
        var parsed = ParseSuccess("\"김,이름\",kim@gmail.com,010-0000-0000,2000-01-01");

        parsed.Rows.Single().Name.Should().Be("김,이름");
    }

    [Fact]
    public void Parse_NewlineInsideQuotes_KeepsNewlineAndUsesStartLine()
    {
        // 레코드가 시작하는 물리 줄 번호를 쓰고, 따옴표 안 줄바꿈도 다음 레코드의 줄 번호에 센다.
        var parsed = ParseSuccess("\"김\n이름\",kim@gmail.com,010-0000-0000,2000-01-01\r\n\"a\r\nb\",x,y,z\ne,f,g,h");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(
            new ImportRow(1, "김\n이름", "kim@gmail.com", "010-0000-0000", "2000-01-01"),
            new ImportRow(3, "a\r\nb", "x", "y", "z"),
            new ImportRow(5, "e", "f", "g", "h"));
    }

    [Fact]
    public void Parse_DoubledQuoteInsideQuotes_IsOneQuote()
    {
        var parsed = ParseSuccess("\"김\"\"이름\"\"\",\"\",c,d");

        parsed.Rows.Should().Equal(new ImportRow(1, "김\"이름\"", string.Empty, "c", "d"));
    }

    [Fact]
    public void Parse_Bom_IsStrippedFromFirstField()
    {
        var content = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(Example)).ToArray();

        var parsed = CsvImportParser.Parse(content).Value;

        parsed.Rows.Should().Equal(new ImportRow(1, "김이름", "kim@gmail.com", "010-0000-0000", "2000-01-01"));
    }

    // ---- 실패: 행 오류 ----

    [Fact]
    public void Parse_TooFewColumns_ReturnsColumnCountMismatch()
    {
        var parsed = ParseSuccess("김이름,kim@gmail.com,010-0000-0000");

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvColumnCountMismatch));
    }

    [Fact]
    public void Parse_TooManyColumns_ReturnsColumnCountMismatch()
    {
        var parsed = ParseSuccess(Example + ",extra");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvColumnCountMismatch));
    }

    [Fact]
    public void Parse_TrailingComma_IsFifthEmptyColumn()
    {
        var parsed = ParseSuccess(Example + ",");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvColumnCountMismatch));
    }

    [Fact]
    public void Parse_QuotedEmptyFieldAlone_IsNotBlankLine()
    {
        // 빈 줄은 따옴표 없이 비었거나 공백만 있는 줄이다. "" 하나는 열 1개인 레코드다.
        var parsed = ParseSuccess("\"\"");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvColumnCountMismatch));
    }

    [Fact]
    public void Parse_UnclosedQuote_ReturnsUnclosedQuoteAtStartLine()
    {
        var parsed = ParseSuccess("a,b,c,d\n\n\"김이름,kim@gmail.com\n010,2000-01-01\n");

        parsed.Rows.Should().Equal(new ImportRow(1, "a", "b", "c", "d"));
        parsed.Errors.Should().Equal(new ImportRowError(3, EmployeeErrors.CsvUnclosedQuote));
    }

    [Fact]
    public void Parse_QuoteInsideUnquotedField_ReturnsUnexpectedQuote()
    {
        var parsed = ParseSuccess("김\"이름,kim@gmail.com,010-0000-0000,2000-01-01");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvUnexpectedQuote));
    }

    [Fact]
    public void Parse_TextAfterClosingQuote_ReturnsUnexpectedQuote()
    {
        var parsed = ParseSuccess("\"김\"이름,kim@gmail.com,010-0000-0000,2000-01-01");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvUnexpectedQuote));
    }

    [Fact]
    public void Parse_UnexpectedQuote_RecoversAtNextLineAndIgnoresLaterQuotesInLine()
    {
        // 행 오류가 나면 그 물리 줄 끝까지 건너뛴다. 같은 줄의 뒤 따옴표가 다음 줄을 삼키지 않는다.
        var parsed = ParseSuccess("a\"b,\"c\ne,f,g,h");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvUnexpectedQuote));
        parsed.Rows.Should().Equal(new ImportRow(2, "e", "f", "g", "h"));
    }

    [Fact]
    public void Parse_UnexpectedQuoteAfterMultiLineQuotedField_ReportsStartLineAndRecoversAtNextLine()
    {
        // 따옴표 안 줄바꿈으로 2줄에 걸친 레코드에서 21021이 나면 시작 줄로 보고하고, 오류가 난 물리 줄 다음부터 다시 읽는다.
        var parsed = ParseSuccess("\"a\r\nb\"x,\"c,d,e\nf,g,h,i");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvUnexpectedQuote));
        parsed.Rows.Should().Equal(new ImportRow(3, "f", "g", "h", "i"));
    }

    [Fact]
    public void Parse_RowWithQuoteErrorAndWrongColumnCount_ReportsQuoteErrorOnly()
    {
        var parsed = ParseSuccess("a\"b,c");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvUnexpectedQuote));
    }

    [Fact]
    public void Parse_SeveralBadRows_CollectsAllErrorsAndGoodRows()
    {
        var parsed = ParseSuccess("a,b,c\n\ne,f,g,h\ni\"j,k,l,m\nn,o,p,q,r\n\"s,t,u,v");

        parsed.Rows.Should().Equal(new ImportRow(3, "e", "f", "g", "h"));
        parsed.Errors.Should().Equal(
            new ImportRowError(1, EmployeeErrors.CsvColumnCountMismatch),
            new ImportRowError(4, EmployeeErrors.CsvUnexpectedQuote),
            new ImportRowError(5, EmployeeErrors.CsvColumnCountMismatch),
            new ImportRowError(6, EmployeeErrors.CsvUnclosedQuote));
    }

    [Fact]
    public void Parse_RowErrors_MessagesDoNotContainInput()
    {
        // NFR-04: 오류 메시지에 입력 값이 없다.
        var parsed = ParseSuccess("kim,b,c\n\"kim\"x,b,c,d\n\"kim");

        parsed.Errors.Should().HaveCount(3);
        parsed.Errors.Should().OnlyContain(error => !error.Error.Message.Contains("kim", StringComparison.Ordinal));
    }

    // ---- 실패: 요청 전체 오류 ----

    [Fact]
    public void Parse_Cp949Bytes_ReturnsImportInvalidUtf8()
    {
        // "김이름,a,b,c"를 CP949로 인코딩한 바이트.
        byte[] content = [0xB1, 0xE8, 0xC0, 0xCC, 0xB8, 0xA7, 0x2C, 0x61, 0x2C, 0x62, 0x2C, 0x63];

        var result = CsvImportParser.Parse(content);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.ImportInvalidUtf8);
    }

    [Fact]
    public void Parse_Utf8EncodedSurrogate_ReturnsImportInvalidUtf8()
    {
        // ED A0 80은 U+D800을 UTF-8로 인코딩한 바이트다(엄격 UTF-8에서 금지).
        byte[] content = [0x61, 0xED, 0xA0, 0x80, 0x2C, 0x62, 0x2C, 0x63, 0x2C, 0x64];

        CsvImportParser.Parse(content).Error.Should().BeSameAs(EmployeeErrors.ImportInvalidUtf8);
    }

    [Fact]
    public void Parse_OverMaxRows_ReturnsImportTooManyRows()
    {
        var result = CsvImportParser.Parse(Encoding.UTF8.GetBytes(Rows(ImportLimits.MaxRows + 1)));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.ImportTooManyRows);
    }

    [Fact]
    public void Parse_OverMaxRowsCountingErrorRows_ReturnsImportTooManyRows()
    {
        // 행 오류가 난 레코드도 행 수에 센다.
        var result = CsvImportParser.Parse(Encoding.UTF8.GetBytes(Rows(ImportLimits.MaxRows) + "a,b\n"));

        result.Error.Should().BeSameAs(EmployeeErrors.ImportTooManyRows);
    }

    [Fact]
    public void Parse_OverMaxRowsEndingWithUnclosedQuote_ReturnsImportTooManyRows()
    {
        var result = CsvImportParser.Parse(Encoding.UTF8.GetBytes(Rows(ImportLimits.MaxRows) + "\"a,b,c,d"));

        result.Error.Should().BeSameAs(EmployeeErrors.ImportTooManyRows);
    }

    // ---- 엣지 ----

    [Fact]
    public void Parse_ExactlyMaxRowsWithBlankLines_Succeeds()
    {
        // 빈 줄은 행 수에 세지 않는다(줄 번호에만 센다).
        var text = Rows(ImportLimits.MaxRows).Replace("\n", "\n\n", StringComparison.Ordinal);

        var parsed = ParseSuccess(text);

        ImportLimits.MaxRows.Should().Be(1000);
        parsed.Rows.Should().HaveCount(1000);
        parsed.Rows[^1].RowNumber.Should().Be(1999);
    }

    [Fact]
    public void Parse_Empty_ReturnsNoRows()
    {
        // 빈 입력(21028) 판정은 Validator 몫이다.
        var parsed = ParseSuccess(string.Empty);

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Parse_BlankAndWhitespaceOnlyLines_ReturnNoRows()
    {
        // 공백만 있는 줄은 필드 trim 뒤 빈 줄이므로 무시한다.
        var parsed = ParseSuccess("\n \t\r\n\r\n   ");

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Parse_FourEmptyColumns_IsRowWithEmptyFields()
    {
        // 빈 필드의 필수 판정은 Value Object 몫이다.
        var parsed = ParseSuccess(",,,");

        parsed.Rows.Should().Equal(new ImportRow(1, string.Empty, string.Empty, string.Empty, string.Empty));
    }

    [Fact]
    public void Parse_CrAtLineEnd_IsTrimmed()
    {
        // 줄 끝은 \n · \r\n뿐이다. 마지막 줄 끝의 CR 단독은 필드 안 문자이고 trim으로 지워진다.
        var parsed = ParseSuccess("a,b,c,d\r");

        parsed.Rows.Should().Equal(new ImportRow(1, "a", "b", "c", "d"));
    }

    [Fact]
    public void Parse_CrInsideField_IsKept()
    {
        var parsed = ParseSuccess("a\rb,c,d,e");

        parsed.Rows.Single().Name.Should().Be("a\rb");
    }

    [Fact]
    public void Parse_CrOnlyLineSeparator_IsNotLineBreak()
    {
        // CR만으로 나눈 두 줄은 한 물리 줄이고 한 레코드(열 7개)다.
        var parsed = ParseSuccess("a,b,c,d\re,f,g,h\ni,j,k,l");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.CsvColumnCountMismatch));
        parsed.Rows.Should().Equal(new ImportRow(2, "i", "j", "k", "l"));
    }

    [Fact]
    public void Parse_CrOnlyLine_IsBlankLine()
    {
        // CR 하나뿐인 줄은 trim 뒤 비므로 빈 줄로 무시하되 줄 번호에는 센다.
        var parsed = ParseSuccess("\r\na,b,c,d\n\r");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(new ImportRow(2, "a", "b", "c", "d"));
    }

    [Fact]
    public void Parse_CrAfterClosingQuote_IsIgnoredAsWhitespace()
    {
        var parsed = ParseSuccess("a,b,c,\"d\"\r");

        parsed.Rows.Should().Equal(new ImportRow(1, "a", "b", "c", "d"));
    }

    [Theory]
    [MemberData(nameof(ControlCharacterFields), DisableDiscoveryEnumeration = true)]
    public void Parse_ControlCharacterInsideField_IsPreserved(string name, char control)
    {
        // NUL 등 제어 문자의 거부는 Value Object 몫이다. 파서는 값을 보존한다.
        name.Should().HaveLength(3).And.Contain(control.ToString(), "테스트 데이터가 직렬화로 바뀌지 않아야 한다");

        var parsed = ParseSuccess(name + ",b,c,d");

        parsed.Rows.Single().Name.Should().Be(name);
        parsed.Rows.Single().Name!.Should().HaveLength(3);
        parsed.Rows.Single().Name![1].Should().Be(control);
    }

    [Fact]
    public void Parse_NulByte_IsPreservedInField()
    {
        byte[] content = [0x61, 0x00, 0x62, 0x2C, 0x62, 0x2C, 0x63, 0x2C, 0x64];

        var parsed = CsvImportParser.Parse(content).Value;

        parsed.Rows.Single().Name.Should().Be("a\0b");
    }

    [Fact]
    public void Parse_HeaderLine_IsTreatedAsData()
    {
        // 헤더 없음(TD-028): 첫 줄도 데이터다. 날짜 형식 판정은 Value Object 몫.
        var parsed = ParseSuccess("name,email,tel,joined\n" + Example);

        parsed.Rows.Select(row => row.Name).Should().Equal("name", "김이름");
    }

    private static ImportParseResult ParseSuccess(string text)
    {
        Result<ImportParseResult> result = CsvImportParser.Parse(Encoding.UTF8.GetBytes(text));

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static string Rows(int count) =>
        string.Concat(Enumerable.Range(1, count).Select(index => $"n{index},e{index}@x.com,010-0000-0000,2000-01-01\n"));
}
