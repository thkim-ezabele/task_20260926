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

    [Fact]
    public void Fields_MatchDocumentedTable()
    {
        Fields.Select(field => (field.Name, field.Error.Code, field.Error.Type)).Should().BeEquivalentTo(new[]
        {
            ("DisplayNameRequired", 21001, ErrorType.Validation),
            ("DisplayNameTooLong", 21002, ErrorType.Validation),
            ("EmailRequired", 21003, ErrorType.Validation),
            ("EmailInvalid", 21004, ErrorType.Validation),
            ("EmailTooLong", 21005, ErrorType.Validation),
            ("EmployeeStatusRequired", 21006, ErrorType.Validation),
            ("NotFound", 22001, ErrorType.NotFound),
            ("DuplicateEmail", 23001, ErrorType.Conflict),
        });
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
