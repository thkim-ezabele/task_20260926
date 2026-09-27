using System.Reflection;
using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Errors;

// 원본: wiki/05-api/error-codes.md "공통 에러 코드". 표를 바꾸면 이 데이터도 함께 바꾼다.
public sealed class CommonErrorsTests
{
    private static readonly (string Name, int Code, ErrorType Type)[] Documented =
    [
        (nameof(CommonErrors.ValidationFailed), 1001, ErrorType.Validation),
        (nameof(CommonErrors.InvalidCode), 1002, ErrorType.Validation),
        (nameof(CommonErrors.InvalidPaging), 1003, ErrorType.Validation),
        (nameof(CommonErrors.NotFound), 2001, ErrorType.NotFound),
        (nameof(CommonErrors.ConcurrencyConflict), 3001, ErrorType.Conflict),
        (nameof(CommonErrors.DuplicateRequest), 3002, ErrorType.Conflict),
        (nameof(CommonErrors.UniqueConstraintViolated), 3003, ErrorType.Conflict),
        (nameof(CommonErrors.Unauthenticated), 5001, ErrorType.Unauthorized),
        (nameof(CommonErrors.Forbidden), 5002, ErrorType.Forbidden),
        (nameof(CommonErrors.Unexpected), 9001, ErrorType.Internal),
        (nameof(CommonErrors.ExternalServiceFailed), 9002, ErrorType.External),
        (nameof(CommonErrors.TemporarilyUnavailable), 9003, ErrorType.Unavailable),
    ];

    public static TheoryData<string, int, ErrorType> DocumentedCommonErrors()
    {
        var data = new TheoryData<string, int, ErrorType>();
        foreach (var (name, code, type) in Documented)
        {
            data.Add(name, code, type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DocumentedCommonErrors))]
    public void Field_ForEachDocumentedCode_HasDocumentedCodeAndType(string name, int code, ErrorType type)
    {
        var error = GetError(name);

        error.Code.Should().Be(code);
        error.Type.Should().Be(type);
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(DocumentedCommonErrors))]
    public void Field_ForEachDocumentedCode_HasCommonServiceDigitAndTypeDigitEqualToTypeValueDividedByTen(
        string name, int code, ErrorType type)
    {
        var error = GetError(name);

        (error.Code / 10000).Should().Be(0, "공통 코드의 서비스 자리는 0이다: {0}", code);
        (error.Code / 1000 % 10).Should().Be((short)type / 10);
    }

    [Fact]
    public void Fields_Always_MatchDocumentedTableExactly()
    {
        var documented = Documented.Select(row => row.Name);

        var names = CommonErrorFields().Select(field => field.Name);

        names.Should().BeEquivalentTo(documented);
    }

    [Fact]
    public void Fields_Always_HaveUniqueCodes()
    {
        var codes = CommonErrorFields().Select(field => ((Error)field.GetValue(null)!).Code);

        codes.Should().OnlyHaveUniqueItems();
    }

    private static Error GetError(string name) =>
        (Error)typeof(CommonErrors).GetField(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    private static IEnumerable<FieldInfo> CommonErrorFields() =>
        typeof(CommonErrors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => typeof(Error).IsAssignableFrom(field.FieldType));
}
