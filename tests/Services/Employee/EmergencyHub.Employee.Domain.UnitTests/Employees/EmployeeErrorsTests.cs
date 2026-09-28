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
        ("NotFound", 22001, ErrorType.NotFound),
        ("DuplicateEmail", 23001, ErrorType.Conflict),
    ];

    // '폐기' 행. 상수는 PRD-001 샘플이 쓰므로 S05-T04에서 지운다(그때 이 목록을 비운다).
    private static readonly (string Name, int Code, ErrorType Type)[] DeprecatedRows =
    [
        ("DisplayNameRequired", 21001, ErrorType.Validation),
        ("DisplayNameTooLong", 21002, ErrorType.Validation),
        ("EmployeeStatusRequired", 21006, ErrorType.Validation),
    ];

    // '예약' 행(21018 ~ 21030, 23002). 구현 작업(S06-T02 ~ T04)이 상수를 추가하면서 '사용'으로 옮긴다.
    private static readonly int[] ReservedCodes =
    [
        21018, 21019, 21020, 21021, 21022, 21023, 21024, 21025, 21026, 21027, 21028, 21029, 21030, 23002,
    ];

    [Fact]
    public void Fields_MatchDocumentedUsedAndDeprecatedRows()
    {
        Fields.Select(field => (field.Name, field.Error.Code, field.Error.Type))
            .Should().BeEquivalentTo(UsedRows.Concat(DeprecatedRows));
    }

    [Fact]
    public void DocumentedRows_CountByStatus()
    {
        // S05-T03 기준선: 사용 16 · 폐기 3 · 예약 14(error-codes Employee 표 행 수 33).
        UsedRows.Should().HaveCount(16);
        DeprecatedRows.Should().HaveCount(3);
        ReservedCodes.Should().HaveCount(14).And.OnlyHaveUniqueItems();
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
