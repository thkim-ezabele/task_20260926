using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Errors;

// ADR-0028 "상세 Conflict 오류": 상세 항목은 경로(FieldError.PropertyName과 같은 규칙) · 정수 코드 · 메시지, 코드는 Conflict 유형 Error에서만.
public sealed class ConflictDetailTests
{
    private static readonly Error DuplicateEmail = Error.Conflict(23001, "이미 등록된 이메일입니다.");

    public static TheoryData<Error> NonConflictErrors() =>
        new(CommonErrors.ValidationFailed, CommonErrors.PayloadTooLarge, CommonErrors.UnsupportedMediaType, CommonErrors.NotFound, CommonErrors.Unexpected);

    // ---- 성공 ----

    [Fact]
    public void Create_WithConflictError_CopiesPropertyNameCodeAndMessage()
    {
        var detail = ConflictDetail.Create("Rows[3].Email", DuplicateEmail);

        detail.PropertyName.Should().Be("Rows[3].Email");
        detail.Code.Should().Be(23001);
        detail.Message.Should().Be("이미 등록된 이메일입니다.");
    }

    [Fact]
    public void Create_WithCommonConflictError_UsesCommonCode()
    {
        var detail = ConflictDetail.Create("Rows[1].Email", CommonErrors.UniqueConstraintViolated);

        detail.Code.Should().Be(3003);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var left = ConflictDetail.Create("Rows[3].Email", DuplicateEmail);
        var right = ConflictDetail.Create("Rows[3].Email", DuplicateEmail);

        var result = left.Equals(right);

        result.Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    // ---- 실패 ----

    [Fact]
    public void Create_WithNullPropertyName_ThrowsArgumentNullException()
    {
        var act = () => ConflictDetail.Create(null!, DuplicateEmail);

        act.Should().Throw<ArgumentNullException>().WithParameterName("propertyName");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WithWhitespacePropertyName_ThrowsArgumentException(string path)
    {
        var act = () => ConflictDetail.Create(path, DuplicateEmail);

        act.Should().Throw<ArgumentException>().WithParameterName("propertyName");
    }

    [Fact]
    public void Create_WithNullError_ThrowsArgumentNullException()
    {
        var act = () => ConflictDetail.Create("Rows[3].Email", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    [Theory]
    [MemberData(nameof(NonConflictErrors))]
    public void Create_WithNonConflictError_ThrowsArgumentException(Error nonConflictError)
    {
        var act = () => ConflictDetail.Create("Rows[3].Email", nonConflictError);

        act.Should().Throw<ArgumentException>().WithParameterName("error");
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData("Rows[100].Email")]
    [InlineData("Email")]
    [InlineData("")]
    public void Create_WithRowOrObjectLevelPropertyName_KeepsPropertyNameAsIs(string path)
    {
        var detail = ConflictDetail.Create(path, DuplicateEmail);

        detail.PropertyName.Should().Be(path);
    }
}
