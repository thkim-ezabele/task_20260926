using System.Reflection;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// 원본은 wiki/05-api/error-codes.md "Employee 에러 코드" 표. 코드 · 유형과 필드 목록을 전수 대조한다(S03 계획 리뷰 코드 선배정).
public sealed class EmployeeErrorsTests
{
    private static readonly IReadOnlyList<(string Name, Error Error)> Fields = typeof(EmployeeErrors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(Error))
        .Select(field => (field.Name, (Error)field.GetValue(null)!))
        .ToList();

    // error-codes Employee 표의 '사용' 행(1:1). 판정 원본이 Value Object인 필드 코드(21003 ~ 21005, 21007 ~ 21017)를 포함한다.
    private static readonly (string Name, int Code, ErrorType Type)[] UsedRows =
    [
        ("EmailRequired", 21003, ErrorType.Validation),
        ("EmailInvalid", 21004, ErrorType.Validation),
        ("EmailTooLong", 21005, ErrorType.Validation),
        ("NameRequired", 21007, ErrorType.Validation),
        ("NameTooLong", 21008, ErrorType.Validation),
        ("NameInvalidCharacter", 21009, ErrorType.Validation),
        ("PhoneNumberRequired", 21010, ErrorType.Validation),
        ("PhoneNumberInvalidCharacter", 21011, ErrorType.Validation),
        ("PhoneNumberDigitCountOutOfRange", 21012, ErrorType.Validation),
        ("PhoneNumberTooLong", 21013, ErrorType.Validation),
        ("PhoneNumberInvalidHyphen", 21014, ErrorType.Validation),
        ("JoinedOnRequired", 21015, ErrorType.Validation),
        ("JoinedOnInvalidFormat", 21016, ErrorType.Validation),
        ("JoinedOnTooEarly", 21017, ErrorType.Validation),
        ("DuplicateEmailInRequest", 21018, ErrorType.Validation),
        ("CsvColumnCountMismatch", 21019, ErrorType.Validation),
        ("CsvUnclosedQuote", 21020, ErrorType.Validation),
        ("CsvUnexpectedQuote", 21021, ErrorType.Validation),
        ("ImportInvalidUtf8", 21022, ErrorType.Validation),
        ("JsonSyntaxInvalid", 21023, ErrorType.Validation),
        ("JsonItemNotObject", 21024, ErrorType.Validation),
        ("JsonValueNotString", 21025, ErrorType.Validation),
        ("JsonDuplicateProperty", 21026, ErrorType.Validation),
        ("ImportTooManyRows", 21027, ErrorType.Validation),
        ("ImportInputEmpty", 21028, ErrorType.Validation),
        ("ImportMultipleSources", 21029, ErrorType.Validation),
        ("RowErrorsTruncated", 21030, ErrorType.Validation),
        ("NotFound", 22001, ErrorType.NotFound),
        ("DuplicateEmail", 23001, ErrorType.Conflict),
        ("RowConflictsTruncated", 23002, ErrorType.Conflict),
    ];

    // '폐기' 행(21001 · 21002 · 21006). 상수는 S05-T04에서 지웠고 번호는 재사용하지 않는다.
    private static readonly int[] DeprecatedCodes = [21001, 21002, 21006];

    // '예약' 행. 구현 작업이 상수를 추가하면서 '사용'으로 옮긴다(S06-T02가 21019 ~ 21022 · 21027, S06-T03이 21023 ~ 21026,
    // S06-T04가 21018 · 21028 ~ 21030 · 23002를 옮겨 지금은 없음). 새 예약 행이 생기면 여기에 넣는다.
    private static readonly int[] ReservedCodes = [];

    [Fact]
    public void Fields_MatchDocumentedUsedRows()
    {
        Fields.Select(field => (field.Name, field.Error.Code, field.Error.Type))
            .Should().BeEquivalentTo(UsedRows);
    }

    [Fact]
    public void DeprecatedCodes_HaveNoConstants()
    {
        // 폐기 코드는 상수를 지운 뒤에도 표에 남는다(재사용 금지). 상수가 다시 생기면 실패한다.
        Fields.Select(field => field.Error.Code).Should().NotIntersectWith(DeprecatedCodes);
    }

    [Fact]
    public void DocumentedRows_CountByStatus()
    {
        // S06-T04 기준선: 사용 30 · 폐기 3(상수 없음) · 예약 0(error-codes Employee 표 행 수 33).
        UsedRows.Should().HaveCount(30);
        DeprecatedCodes.Should().HaveCount(3).And.OnlyHaveUniqueItems().And.NotIntersectWith(ReservedCodes);
        ReservedCodes.Should().BeEmpty();
    }

    [Fact]
    public void ReservedCodes_AreNotDefinedYet()
    {
        Fields.Select(field => field.Error.Code).Should().NotIntersectWith(ReservedCodes);
    }

    [Fact]
    public void Codes_AreInsideEmployeeRangeAndUnique()
    {
        Fields.Should().NotBeEmpty();
        Fields.Should().OnlyContain(field => field.Error.Code >= 21001 && field.Error.Code <= 29999);
        Fields.Select(field => field.Error.Code).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Messages_DoNotContainPlaceholdersOrValues()
    {
        // 메시지는 고정 문구다. 이메일 등 입력 값 · 제약 이름을 담지 않는다(개인정보, database.md "영속성 예외 변환").
        Fields.Should().OnlyContain(field => !field.Error.Message.Contains('{') && !field.Error.Message.Contains('@'));
        Fields.Should().OnlyContain(field => !field.Error.Message.Contains("ux_", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateEmail_IsSingleSharedInstance()
    {
        // Handler 사전 검사와 T02 UoW 23505 매핑이 같은 인스턴스를 쓴다(인계 메모). 속성이 아니라 static readonly 필드여야 한다.
        typeof(EmployeeErrors).GetField(nameof(EmployeeErrors.DuplicateEmail))!.IsInitOnly.Should().BeTrue();
        EmployeeErrors.DuplicateEmail.Should().BeSameAs(EmployeeErrors.DuplicateEmail);
    }
}
