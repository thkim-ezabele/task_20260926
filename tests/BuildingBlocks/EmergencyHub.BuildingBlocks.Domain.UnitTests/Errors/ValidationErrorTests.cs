using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Errors;

public sealed class ValidationErrorTests
{
    private static readonly FieldError EmailError =
        FieldError.Create("Email", Error.Validation(21001, "이메일 형식이 아닙니다."));

    private static readonly FieldError ChannelError =
        FieldError.Create("NotificationChannels", CommonErrors.InvalidCode);

    [Fact]
    public void Create_WithFieldErrors_ReturnsValidationFailedError()
    {
        var error = ValidationError.Create([EmailError]);

        error.Code.Should().Be(1001);
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Be("요청 값이 올바르지 않습니다.");
        error.Errors.Should().Equal(EmailError);
    }

    [Fact]
    public void Create_WithFieldErrors_IsAnError()
    {
        var error = ValidationError.Create([EmailError]);

        error.Should().BeAssignableTo<Error>();
    }

    [Fact]
    public void Create_WithMultipleFieldErrors_KeepsOrderAndDuplicates()
    {
        var sameFieldOther = FieldError.Create("Email", Error.Validation(21002, "이메일이 너무 깁니다."));

        var error = ValidationError.Create([ChannelError, EmailError, sameFieldOther, EmailError]);

        error.Errors.Should().Equal(ChannelError, EmailError, sameFieldOther, EmailError);
    }

    [Fact]
    public void Create_WithNullCollection_ThrowsArgumentNullException()
    {
        var act = () => ValidationError.Create(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("errors");
    }

    [Fact]
    public void Create_WithEmptyCollection_ThrowsArgumentException()
    {
        var act = () => ValidationError.Create([]);

        act.Should().Throw<ArgumentException>().WithParameterName("errors");
    }

    [Fact]
    public void Create_WithNullElement_ThrowsArgumentException()
    {
        var act = () => ValidationError.Create([EmailError, null!]);

        act.Should().Throw<ArgumentException>().WithParameterName("errors");
    }

    [Fact]
    public void Create_WhenSourceListChangesAfterward_KeepsOriginalErrors()
    {
        var source = new List<FieldError> { EmailError };
        var error = ValidationError.Create(source);

        source.Add(ChannelError);

        error.Errors.Should().Equal(EmailError);
    }

    [Fact]
    public void Errors_WhenCastToMutableList_ThrowsNotSupportedException()
    {
        var error = ValidationError.Create([EmailError]);
        var list = (IList<FieldError>)error.Errors;

        var act = () => list[0] = ChannelError;

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Equals_WithSameFieldErrors_ReturnsTrue()
    {
        var left = ValidationError.Create([EmailError, ChannelError]);
        var right = ValidationError.Create([EmailError, ChannelError]);

        var result = left.Equals(right);

        result.Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentFieldErrors_ReturnsFalse()
    {
        var left = ValidationError.Create([EmailError]);
        var right = ValidationError.Create([ChannelError]);

        var result = left.Equals(right);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentFieldErrorOrder_ReturnsFalse()
    {
        var left = ValidationError.Create([EmailError, ChannelError]);
        var right = ValidationError.Create([ChannelError, EmailError]);

        var result = left.Equals(right);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithPlainValidationFailedError_ReturnsFalse()
    {
        var error = ValidationError.Create([EmailError]);

        var result = error.Equals(CommonErrors.ValidationFailed);

        result.Should().BeFalse();
        CommonErrors.ValidationFailed.Equals(error).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var error = ValidationError.Create([EmailError]);

        var result = error.Equals(null);

        result.Should().BeFalse();
    }
}
