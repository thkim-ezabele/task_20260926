using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Errors;

public sealed class FieldErrorTests
{
    private static readonly Error InvalidEmail = Error.Validation(21001, "이메일 형식이 아닙니다.");

    [Fact]
    public void Create_WithValidationError_CopiesPropertyNameCodeAndMessage()
    {
        var fieldError = FieldError.Create("Email", InvalidEmail);

        fieldError.PropertyName.Should().Be("Email");
        fieldError.Code.Should().Be(21001);
        fieldError.Message.Should().Be("이메일 형식이 아닙니다.");
    }

    [Theory]
    [InlineData("Address.City")]
    [InlineData("Items[0].Name")]
    [InlineData("")]
    public void Create_WithNestedOrObjectLevelPropertyName_KeepsPropertyNameAsIs(string path)
    {
        var fieldError = FieldError.Create(path, InvalidEmail);

        fieldError.PropertyName.Should().Be(path);
    }

    [Fact]
    public void Create_WithNullPropertyName_ThrowsArgumentNullException()
    {
        var act = () => FieldError.Create(null!, InvalidEmail);

        act.Should().Throw<ArgumentNullException>().WithParameterName("propertyName");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WithWhitespacePropertyName_ThrowsArgumentException(string path)
    {
        var act = () => FieldError.Create(path, InvalidEmail);

        act.Should().Throw<ArgumentException>().WithParameterName("propertyName");
    }

    [Fact]
    public void Create_WithNullError_ThrowsArgumentNullException()
    {
        var act = () => FieldError.Create("Email", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    [Theory]
    [MemberData(nameof(NonValidationErrors))]
    public void Create_WithNonValidationError_ThrowsArgumentException(Error nonValidationError)
    {
        var act = () => FieldError.Create("Email", nonValidationError);

        act.Should().Throw<ArgumentException>().WithParameterName("error");
    }

    [Fact]
    public void Create_WithCommonInvalidCode_UsesCommonCode()
    {
        var fieldError = FieldError.Create("NotificationChannels", CommonErrors.InvalidCode);

        fieldError.Code.Should().Be(1002);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var left = FieldError.Create("Email", InvalidEmail);
        var right = FieldError.Create("Email", InvalidEmail);

        var result = left.Equals(right);

        result.Should().BeTrue();
    }

    public static TheoryData<Error> NonValidationErrors() =>
        new(
            CommonErrors.NotFound,
            CommonErrors.ConcurrencyConflict,
            CommonErrors.Forbidden,
            CommonErrors.Unexpected,
            CommonErrors.PayloadTooLarge,
            CommonErrors.UnsupportedMediaType);
}
