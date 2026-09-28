using System.Text;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Application.Employees.Import;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Import;

// 원본: PRD-002 FR-04 인수 조건, ADR-0026 3 · 5 · 6절. 세 형태(배열 · 단일 객체 · 대괄호 없는 나열), 항목 번호 1부터.
public sealed class JsonImportParserTests
{
    private const string Item = """{"name":"김이름","email":"kim@gmail.com","tel":"010-0000-0000","joined":"2000-01-01"}""";
    private const string OtherItem = """{"name":"이이름","email":"lee@gmail.com","tel":"010-1111-1111","joined":"2001-02-03"}""";

    private static readonly ImportRow ItemRow = new(1, "김이름", "kim@gmail.com", "010-0000-0000", "2000-01-01");
    private static readonly ImportRow OtherItemRow = new(2, "이이름", "lee@gmail.com", "010-1111-1111", "2001-02-03");

    // 짝 없는 서로게이트 이스케이프. 문자열 값은 ASCII 이스케이프 원문이지만 BL-131 규칙대로 발견 단계 열거를 끄고 입력 보존을 단언한다.
    public static TheoryData<string, string> UnpairedSurrogates => new()
    {
        { "값의 높은 서로게이트 단독", """{"name":"\ud800"}""" },
        { "값의 낮은 서로게이트 단독", """{"name":"\udc00"}""" },
        { "값의 높은 서로게이트 뒤 일반 문자", """{"email":"\ud800x"}""" },
        { "값의 높은 서로게이트 두 개", """{"tel":"\ud800\ud800"}""" },
        { "값의 순서가 뒤바뀐 쌍", """{"joined":"\udc00\ud800"}""" },
        { "알려진 속성 이름", """{"n\ud800ame":"a"}""" },
        { "알 수 없는 속성 이름", """{"x\udc00":"a"}""" },
        { "두 번째 항목의 값", """[{"name":"a"},{"name":"\udfff"}]""" },
        { "앞 항목이 항목 오류인 뒤", """[{"joined":1},{"name":"\ud800"}]""" },
        { "같은 항목의 항목 오류 뒤 값", """{"joined":1,"name":"\ud800"}""" },
    };

    // 이스케이프로 넣은 제어 문자. 파서는 값을 보존한다(거부는 Value Object 몫). BL-131: 기대 값에 제어 문자가 있어 발견 단계 열거를 끈다.
    public static TheoryData<string, string> EscapedControlCharacters => new()
    {
        { """a\u0000b""", "a\0b" },
        { """a\u0001b""", "a\u0001b" },
        { """a\u007Fb""", "a\u007Fb" },
        { """a\rb""", "a\rb" },
        { """a\nb""", "a\nb" },
    };

    // 짝이 맞는 서로게이트 이스케이프(U+1F600). BL-131: 기대 값에 서로게이트가 있어 발견 단계 열거를 끈다.
    public static TheoryData<string, string> EscapedSurrogatePairs => new()
    {
        { """{"name":"a\ud83d\ude00"}""", "a\U0001F600" },
        { """{"name":"a\uD83D\uDE00"}""", "a\U0001F600" },
        { """{"x\ud83d\ude00":"b","name":"a\ud83d\ude00"}""", "a\U0001F600" },
    };

    // 문자열 안에 이스케이프 없이 넣은 제어 문자는 JSON 문법 오류다(RFC 8259). BL-131: 입력에 제어 문자가 있어 발견 단계 열거를 끈다.
    public static TheoryData<char> RawControlCharacters => new() { '\0', '\u0001', '\n', '\u001F' };

    public static TheoryData<string, string> SyntaxErrors => new()
    {
        { "배열 끝 쉼표", $"[{Item},]" },
        { "객체 끝 쉼표", """{"name":"a",}""" },
        { "나열 끝 쉼표", $"{Item},{OtherItem}," },
        { "한 줄 주석", $"// 주석\n[{Item}]" },
        { "나열 뒤 한 줄 주석", $"{Item} // 주석" },
        { "블록 주석", $"[/* 주석 */{Item}]" },
        { "배열 두 개 나열", $"[{Item}],[{OtherItem}]" },
        { "배열 두 개 공백 구분", $"[{Item}] [{OtherItem}]" },
        { "나열 사이 쉼표 없음", $"{Item} {OtherItem}" },
        { "닫히지 않은 객체", """{"name":"a" """ },
        { "닫히지 않은 문자열", """{"name":"a}""" },
        { "나열 뒤 닫는 대괄호", $"{Item}]" },
        { "작은따옴표", "{'name':'a'}" },
        { "CSV 텍스트", "김이름,kim@gmail.com,010-0000-0000,2000-01-01" },
        { "루트 숫자", "42" },
        { "루트 문자열", "\"a\"" },
        { "루트 null", "null" },
        { "루트 true", "true" },
        { "잘못된 이스케이프", """{"name":"\x"}""" },
        { "16진수가 아닌 유니코드 이스케이프", """{"name":"\u12G4"}""" },
    };

    public static TheoryData<string, string> NonStringValues => new()
    {
        { "숫자", "20000101" },
        { "소수", "2000.5" },
        { "null", "null" },
        { "true", "true" },
        { "false", "false" },
        { "객체", """{"y":"2000"}""" },
        { "배열", """["2000-01-01"]""" },
    };

    public static TheoryData<string, string> NonObjectItems => new()
    {
        { "null", "null" },
        { "숫자", "1" },
        { "문자열", "\"a\"" },
        { "true", "true" },
        { "배열", $"[{Item}]" },
        { "빈 배열", "[]" },
    };

    // ---- 성공: 세 형태 ----

    [Fact]
    public void Parse_Array_ReturnsRows()
    {
        var parsed = ParseSuccess($"[{Item},{OtherItem}]");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(ItemRow, OtherItemRow);
    }

    [Fact]
    public void Parse_SingleObject_ReturnsOneRow()
    {
        var parsed = ParseSuccess(Item);

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(ItemRow);
    }

    [Fact]
    public void Parse_UnbracketedList_ReturnsRows()
    {
        var parsed = ParseSuccess($"{Item},{OtherItem}");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(ItemRow, OtherItemRow);
    }

    [Theory]
    [InlineData("[{0}]", "{0}")]
    [InlineData("[{0}]", " \t\r\n{0}\r\n ")]
    [InlineData("[\n  {0}\n]", "{0}")]
    public void Parse_ThreeForms_ProduceSameResult(string arrayFormat, string objectFormat)
    {
        // FR-04 인수: 세 형태가 같은 결과. 단일 객체는 나열의 항목 1개인 경우다.
        var fromArray = ParseSuccess(arrayFormat.Replace("{0}", Item, StringComparison.Ordinal));
        var fromObject = ParseSuccess(objectFormat.Replace("{0}", Item, StringComparison.Ordinal));

        fromObject.Rows.Should().Equal(fromArray.Rows);
        fromObject.Errors.Should().Equal(fromArray.Errors);
    }

    [Fact]
    public void Parse_ArrayAndUnbracketedList_ProduceSameResultIncludingItemErrors()
    {
        const string Body = """{"name":"a"},{"joined":20000101},null,{"name":"b","NAME":"c"}""";

        var fromArray = ParseSuccess($"[{Body}]");
        var fromList = ParseSuccess(Body);

        fromList.Rows.Should().Equal(fromArray.Rows);
        fromList.Errors.Should().Equal(fromArray.Errors);
        fromList.Errors.Select(error => error.RowNumber).Should().Equal(2, 3, 4);
    }

    [Fact]
    public void Parse_LeadingWhitespaceBeforeBracket_IsArray()
    {
        var parsed = ParseSuccess($" \r\n\t[{Item}]");

        parsed.Rows.Should().Equal(ItemRow);
    }

    [Fact]
    public void Parse_LeadingBom_IsRemovedBeforeFormDetection()
    {
        // BOM 뒤 첫 문자가 '{'라 나열로 감싼다(BOM이 첫 문자 판정을 막지 않는다).
        byte[] content = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(Item)];

        var result = JsonImportParser.Parse(content);

        result.Value.Rows.Should().Equal(ItemRow);
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsNoRows()
    {
        var parsed = ParseSuccess(" [ ] ");

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void Parse_EmptyOrWhitespaceOnly_ReturnsNoRows(string text)
    {
        // 빈 입력 판정(21028)은 Validator 몫이다. 파서는 CSV와 같이 빈 결과를 돌려준다.
        var parsed = ParseSuccess(text);

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().BeEmpty();
    }

    // ---- 성공: 속성 규칙 ----

    [Fact]
    public void Parse_PropertyNames_AreCaseInsensitive()
    {
        var parsed = ParseSuccess("""{"NAME":"김이름","Email":"kim@gmail.com","tEL":"010-0000-0000","JOINED":"2000-01-01"}""");

        parsed.Rows.Should().Equal(ItemRow);
    }

    [Fact]
    public void Parse_PropertyOrder_DoesNotMatter()
    {
        var parsed = ParseSuccess("""{"joined":"2000-01-01","tel":"010-0000-0000","email":"kim@gmail.com","name":"김이름"}""");

        parsed.Rows.Should().Equal(ItemRow);
    }

    [Fact]
    public void Parse_UnknownProperties_AreIgnored()
    {
        const string Text = """
            {"id":7,"name":"김이름","extra":{"a":[1,2,{"b":null}]},"email":"kim@gmail.com","memo":null,
             "tel":"010-0000-0000","joined":"2000-01-01","flag":true,"list":["x"]}
            """;

        var parsed = ParseSuccess(Text);

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(ItemRow);
    }

    [Fact]
    public void Parse_UnknownPropertiesDifferingOnlyInCase_AreIgnored()
    {
        // 중복 판정은 읽는 속성(name · email · tel · joined)에만 한다. 알 수 없는 속성은 중복이어도 무시한다.
        var parsed = ParseSuccess("""{"memo":"a","MEMO":1,"memo":"b","name":"김이름"}""");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Single().Name.Should().Be("김이름");
    }

    [Fact]
    public void Parse_PropertyNameWithSurroundingSpaces_IsUnknown()
    {
        // 속성 이름은 대소문자만 무시한다. 공백을 붙인 이름은 다른 속성이다.
        var parsed = ParseSuccess("""{" name":"a","name ":"b"}""");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Single().Name.Should().BeNull();
    }

    [Fact]
    public void Parse_MissingProperties_AreNullFields()
    {
        var parsed = ParseSuccess("""{"name":"김이름"}""");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(new ImportRow(1, "김이름", null, null, null));
    }

    [Fact]
    public void Parse_EmptyObject_IsRowWithAllNullFields()
    {
        var parsed = ParseSuccess("{}");

        parsed.Rows.Should().Equal(new ImportRow(1, null, null, null, null));
    }

    [Fact]
    public void Parse_MissingProperty_IsReportedAsFieldRequiredByValueObject()
    {
        // FR-04 "값이 없으면 그 항목의 오류", ADR-0026 5절 "속성이 없으면 그 필드의 필수 코드". 판정 원본은 Value Object Create(null)다.
        var row = ParseSuccess("{}").Rows.Single();

        Name.Create(row.Name).Error.Should().BeSameAs(EmployeeErrors.NameRequired);
        Email.Create(row.Email).Error.Should().BeSameAs(EmployeeErrors.EmailRequired);
        PhoneNumber.Create(row.Tel).Error.Should().BeSameAs(EmployeeErrors.PhoneNumberRequired);
        JoinedOn.Create(row.Joined).Error.Should().BeSameAs(EmployeeErrors.JoinedOnRequired);
    }

    [Fact]
    public void Parse_StringValues_AreNotTrimmed()
    {
        // 앞뒤 공백 처리는 Value Object 몫이다. JSON 문자열은 원문 그대로 넘긴다.
        var parsed = ParseSuccess("""{"name":"  김이름 ","email":"","tel":" ","joined":"\t"}""");

        parsed.Rows.Should().Equal(new ImportRow(1, "  김이름 ", string.Empty, " ", "\t"));
    }

    [Fact]
    public void Parse_EscapedCharacters_AreUnescaped()
    {
        var parsed = ParseSuccess("""{"name":"김\"\\\/😀"}""");

        parsed.Rows.Single().Name.Should().Be("김\"\\/\U0001F600");
    }

    [Fact]
    public void Parse_EscapedPropertyName_IsMatched()
    {
        var parsed = ParseSuccess("""{"n\u0061me":"김이름"}""");

        parsed.Rows.Single().Name.Should().Be("김이름");
    }

    [Theory]
    [MemberData(nameof(EscapedControlCharacters), DisableDiscoveryEnumeration = true)]
    public void Parse_EscapedControlCharacter_IsPreserved(string escaped, string expected)
    {
        escaped.Should().StartWith("a\\").And.EndWith("b", "테스트 데이터가 이스케이프 원문을 담아야 한다");
        expected.Should().HaveLength(3);
        char.IsControl(expected[1]).Should().BeTrue("테스트 데이터가 직렬화로 바뀌지 않아야 한다");

        var parsed = ParseSuccess($$"""{"name":"{{escaped}}"}""");

        parsed.Rows.Single().Name.Should().Be(expected);
        parsed.Rows.Single().Name![1].Should().Be(expected[1]);
    }

    [Fact]
    public void Parse_EscapedNul_IsPreservedInValue()
    {
        // 완료 조건 ④: \u0000은 값 보존(실측). 거부는 Value Object 몫이다.
        var parsed = ParseSuccess("""{"name":"\u0000","email":"a\u0000"}""");

        parsed.Rows.Single().Name.Should().Be("\0");
        parsed.Rows.Single().Email.Should().Be("a\0");
        parsed.Rows.Single().Email!.Should().HaveLength(2);
    }

    [Fact]
    public void Parse_EscapedNulInUnknownPropertyName_IsIgnored()
    {
        var parsed = ParseSuccess("""{"name\u0000":"a"}""");

        parsed.Rows.Single().Name.Should().BeNull();
    }

    [Fact]
    public void Parse_UnpairedSurrogateInUnknownPropertyValue_IsNotRead()
    {
        // 판정 범위: 모든 속성 이름과 읽는 속성의 값. 알 수 없는 속성의 값은 읽지 않으므로 판정하지 않는다.
        var parsed = ParseSuccess("""{"memo":"\ud800","extra":{"\udc00":"\ud800"},"name":"a"}""");

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Single().Name.Should().Be("a");
    }

    [Theory]
    [MemberData(nameof(EscapedSurrogatePairs), DisableDiscoveryEnumeration = true)]
    public void Parse_EscapedSurrogatePair_IsPreserved(string text, string expected)
    {
        // tester 보강(S06-T03 ④ 엣지): 짝이 맞는 이스케이프(대소문자 16진수)는 21022가 아니고 값이 보존된다.
        text.All(character => character < 0x80).Should().BeTrue("테스트 데이터가 직렬화로 바뀌지 않아야 한다");
        expected.Should().HaveLength(3);
        char.IsSurrogatePair(expected[1], expected[2]).Should().BeTrue("기대 값이 짝이 맞는 서로게이트를 담아야 한다");

        var parsed = ParseSuccess(text);

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Single().Name.Should().Be(expected);
    }

    [Fact]
    public void Parse_UppercaseHexUnpairedSurrogateEscape_IsImportInvalidUtf8()
    {
        // tester 보강(S06-T03 ④ 엣지): 16진수 대문자 표기도 같은 판정이다.
        var result = Parse("""{"name":"\uD800"}""");

        result.Error.Should().BeSameAs(EmployeeErrors.ImportInvalidUtf8);
    }

    [Fact]
    public void Parse_UnpairedSurrogateInDuplicatePropertyValue_IsDuplicateItemError()
    {
        // tester 보강(현재 동작 고정): 중복으로 판정된 두 번째 값은 읽지 않으므로 21022가 아니라 항목 오류 21026이다.
        var parsed = ParseSuccess("""{"name":"a","NAME":"\ud800"}""");

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.JsonDuplicateProperty, ImportField.Name));
    }

    [Fact]
    public void Parse_ItemNumbers_StartAtOneAndFollowOrder()
    {
        var parsed = ParseSuccess("""[{"name":"a"},{"name":"b"},{"name":"c"}]""");

        parsed.Rows.Select(row => row.RowNumber).Should().Equal(1, 2, 3);
        parsed.Rows.Select(row => row.Name).Should().Equal("a", "b", "c");
    }

    [Fact]
    public void Parse_MaxRows_IsAccepted()
    {
        var parsed = ParseSuccess(Items(ImportLimits.MaxRows));

        parsed.Rows.Should().HaveCount(ImportLimits.MaxRows);
        parsed.Rows[^1].RowNumber.Should().Be(ImportLimits.MaxRows);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Parse_MaxDepth64_IsAccepted(bool unbracketed)
    {
        // 입력 기준 최대 깊이 64(JsonDocumentOptions 기본값). 나열로 감싼 대괄호는 입력 깊이에 넣지 않는다.
        var text = unbracketed ? NestedObject(64) : "[" + NestedObject(63) + "]";

        var parsed = ParseSuccess(text);

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().Equal(new ImportRow(1, null, null, null, null));
    }

    // ---- 실패: 항목 오류 ----

    [Fact]
    public void Parse_JoinedAsNumber_IsItemErrorOnlyForThatItem()
    {
        // FR-04 인수: 한 항목의 "joined": 20000101(숫자)은 그 항목 오류로만 보고된다.
        var parsed = ParseSuccess($$"""[{{Item}},{"name":"b","email":"b@x.com","tel":"010-0000-0000","joined":20000101},{{Item}}]""");

        parsed.Rows.Select(row => row.RowNumber).Should().Equal(1, 3);
        parsed.Errors.Should().Equal(new ImportRowError(2, EmployeeErrors.JsonValueNotString, ImportField.Joined));
    }

    [Theory]
    [MemberData(nameof(NonStringValues))]
    public void Parse_NonStringValue_IsJsonValueNotString(string description, string value)
    {
        description.Should().NotBeEmpty();

        var parsed = ParseSuccess($$"""{"name":"a","tel":{{value}}}""");

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.JsonValueNotString, ImportField.Tel));
    }

    [Theory]
    [InlineData("name", (int)ImportField.Name)]
    [InlineData("EMAIL", (int)ImportField.Email)]
    [InlineData("Tel", (int)ImportField.Tel)]
    [InlineData("joined", (int)ImportField.Joined)]
    public void Parse_NonStringValue_ReportsThatField(string property, int field)
    {
        var parsed = ParseSuccess($$"""{"{{property}}":1}""");

        parsed.Errors.Single().Field.Should().Be((ImportField)field);
    }

    [Theory]
    [MemberData(nameof(NonObjectItems))]
    public void Parse_NonObjectItem_IsJsonItemNotObject(string description, string item)
    {
        description.Should().NotBeEmpty();

        var parsed = ParseSuccess($"[{Item},{item},{OtherItem}]");

        parsed.Rows.Select(row => row.RowNumber).Should().Equal(1, 3);
        parsed.Errors.Should().Equal(new ImportRowError(2, EmployeeErrors.JsonItemNotObject, ImportField.None));
    }

    [Fact]
    public void Parse_NonObjectItemInUnbracketedList_IsJsonItemNotObject()
    {
        var parsed = ParseSuccess($"{Item},null,1");

        parsed.Errors.Should().Equal(
            new ImportRowError(2, EmployeeErrors.JsonItemNotObject, ImportField.None),
            new ImportRowError(3, EmployeeErrors.JsonItemNotObject, ImportField.None));
    }

    [Theory]
    [InlineData("name", "Name", (int)ImportField.Name)]
    [InlineData("email", "EMAIL", (int)ImportField.Email)]
    [InlineData("TEL", "tel", (int)ImportField.Tel)]
    [InlineData("joined", "jOiNeD", (int)ImportField.Joined)]
    public void Parse_PropertiesDifferingOnlyInCase_IsJsonDuplicateProperty(string first, string second, int field)
    {
        var parsed = ParseSuccess($$"""[{{Item}},{"{{first}}":"a","{{second}}":"b"}]""");

        parsed.Rows.Should().Equal(ItemRow);
        parsed.Errors.Should().Equal(new ImportRowError(2, EmployeeErrors.JsonDuplicateProperty, (ImportField)field));
    }

    [Fact]
    public void Parse_ExactDuplicateProperty_IsJsonDuplicateProperty()
    {
        // 대소문자까지 같은 중복도 어느 값을 쓸지 모호하므로 같은 코드다(JsonDocument는 중복을 막지 않는다).
        var parsed = ParseSuccess("""{"name":"a","name":"a"}""");

        parsed.Errors.Should().Equal(new ImportRowError(1, EmployeeErrors.JsonDuplicateProperty, ImportField.Name));
    }

    [Fact]
    public void Parse_DuplicatePropertyWithNonStringValue_ReportsDuplicate()
    {
        // 중복 판정이 값 형식보다 먼저다(두 번째 속성의 값은 보지 않는다).
        var parsed = ParseSuccess("""{"joined":"2000-01-01","Joined":20000101}""");

        parsed.Errors.Single().Error.Should().BeSameAs(EmployeeErrors.JsonDuplicateProperty);
    }

    [Fact]
    public void Parse_SeveralItemErrorsInOneItem_ReportsFirstInPropertyOrder()
    {
        // 항목마다 첫 오류 하나(CSV 행 오류와 같은 규칙).
        var parsed = ParseSuccess("""[{"tel":1,"name":"a","NAME":"b"},{"name":"a","NAME":"b","tel":1}]""");

        parsed.Errors.Should().Equal(
            new ImportRowError(1, EmployeeErrors.JsonValueNotString, ImportField.Tel),
            new ImportRowError(2, EmployeeErrors.JsonDuplicateProperty, ImportField.Name));
    }

    [Fact]
    public void Parse_ItemErrors_DoNotStopFollowingItems()
    {
        var parsed = ParseSuccess("""[null,{"joined":1},{"name":"a","Name":"b"},{"name":"c"}]""");

        parsed.Errors.Select(error => (error.RowNumber, error.Error.Code)).Should().Equal((1, 21024), (2, 21025), (3, 21026));
        parsed.Rows.Should().Equal(new ImportRow(4, "c", null, null, null));
    }

    // ---- 실패: 요청 전체 오류 ----

    [Theory]
    [MemberData(nameof(SyntaxErrors))]
    public void Parse_SyntaxError_IsJsonSyntaxInvalid(string description, string text)
    {
        description.Should().NotBeEmpty();

        var result = Parse(text);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.JsonSyntaxInvalid);
    }

    [Theory]
    [MemberData(nameof(RawControlCharacters), DisableDiscoveryEnumeration = true)]
    public void Parse_RawControlCharacterInString_IsJsonSyntaxInvalid(char control)
    {
        char.IsControl(control).Should().BeTrue("테스트 데이터가 직렬화로 바뀌지 않아야 한다");

        var result = Parse($$"""{"name":"a{{control}}b"}""");

        result.Error.Should().BeSameAs(EmployeeErrors.JsonSyntaxInvalid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Parse_DepthOver64_IsJsonSyntaxInvalid(bool unbracketed)
    {
        var text = unbracketed ? NestedObject(65) : "[" + NestedObject(64) + "]";

        var result = Parse(text);

        result.Error.Should().BeSameAs(EmployeeErrors.JsonSyntaxInvalid);
    }

    [Theory]
    [MemberData(nameof(UnpairedSurrogates), DisableDiscoveryEnumeration = true)]
    public void Parse_UnpairedSurrogateEscape_IsImportInvalidUtf8(string description, string text)
    {
        // 완료 조건 ④: 500(InvalidOperationException) 없이 요청 전체 오류 21022.
        description.Should().NotBeEmpty();
        text.Should().MatchRegex(@"\\u[dD][89abcdefABCDEF][0-9a-fA-F]{2}", "테스트 데이터가 이스케이프 원문을 담아야 한다");
        text.All(character => character < 0x80).Should().BeTrue("테스트 데이터가 직렬화로 바뀌지 않아야 한다");

        var result = Parse(text);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.ImportInvalidUtf8);
    }

    [Fact]
    public void Parse_InvalidUtf8Bytes_IsImportInvalidUtf8()
    {
        byte[] content = [.. "{\"name\":\""u8, 0xB1, 0xE8, .. "\"}"u8];

        var result = JsonImportParser.Parse(content);

        result.Error.Should().BeSameAs(EmployeeErrors.ImportInvalidUtf8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Parse_MoreThanMaxRows_IsImportTooManyRows(bool unbracketed)
    {
        var items = Items(ImportLimits.MaxRows + 1);

        var result = Parse(unbracketed ? items[1..^1] : items);

        result.Error.Should().BeSameAs(EmployeeErrors.ImportTooManyRows);
    }

    [Fact]
    public void Parse_ItemErrors_AreCountedTowardMaxRows()
    {
        // CSV와 같은 세기 규칙: 항목 오류가 난 항목도 센다.
        var text = "[" + string.Join(",", Enumerable.Repeat("null", ImportLimits.MaxRows)) + ",{}]";

        var result = Parse(text);

        result.Error.Should().BeSameAs(EmployeeErrors.ImportTooManyRows);
    }

    [Fact]
    public void Parse_SyntaxErrorAfterItemErrors_IsRequestError()
    {
        // 문법 오류는 항목 오류보다 우선한다(문서 전체를 먼저 읽는다).
        var result = Parse("""[{"joined":1},null,{"name":"a"},]""");

        result.Error.Should().BeSameAs(EmployeeErrors.JsonSyntaxInvalid);
    }

    // ---- 오류에 원문 · 입력 값 없음 (NFR-04, FR-04) ----

    [Fact]
    public void Parse_Errors_DoNotContainExceptionMessagesOrInput()
    {
        Error[] errors =
        [
            Parse("""[{"name":"kim-secret"},]""").Error,
            Parse("""{"name":"kim-secret\ud800"}""").Error,
            Parse("[" + NestedObject(64) + "]").Error,
            .. ParseSuccess("""[{"name":"kim-secret","joined":20000101},"kim-secret",{"name":"kim-secret","NAME":"x"}]""")
                .Errors.Select(error => error.Error),
        ];

        errors.Should().HaveCount(6);
        errors.Select(error => error.Code).Should().Equal(21023, 21022, 21023, 21025, 21024, 21026);
        errors.Should().OnlyContain(error =>
            !error.Message.Contains("kim", StringComparison.OrdinalIgnoreCase)
            && !error.Message.Contains("LineNumber", StringComparison.Ordinal)
            && !error.Message.Contains("BytePosition", StringComparison.Ordinal)
            && !error.Message.Contains("0xD800", StringComparison.OrdinalIgnoreCase)
            && !error.Message.Contains("surrogate", StringComparison.OrdinalIgnoreCase)
            && !error.Message.Contains('\''));
    }

    private static Result<ImportParseResult> Parse(string text) => JsonImportParser.Parse(Encoding.UTF8.GetBytes(text));

    private static ImportParseResult ParseSuccess(string text)
    {
        var result = Parse(text);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static string Items(int count) =>
        "[" + string.Join(",", Enumerable.Range(1, count).Select(index => $$"""{"name":"n{{index}}"}""")) + "]";

    // 깊이 depth인 객체: {"a":{"a":...{}...}}. 가장 바깥 객체가 항목이다.
    private static string NestedObject(int depth) =>
        string.Concat(Enumerable.Repeat("""{"a":""", depth - 1)) + "{}" + new string('}', depth - 1);
}
