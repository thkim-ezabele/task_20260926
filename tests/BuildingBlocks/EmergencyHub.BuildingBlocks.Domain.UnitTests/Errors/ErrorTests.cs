using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Errors;

public sealed class ErrorTests
{
    private const string Message = "오류 메시지";

    [Theory]
    [InlineData(ErrorType.Validation, 21001)]
    [InlineData(ErrorType.NotFound, 22001)]
    [InlineData(ErrorType.Conflict, 23001)]
    [InlineData(ErrorType.BusinessRule, 24001)]
    [InlineData(ErrorType.Unauthorized, 15001)]
    [InlineData(ErrorType.Forbidden, 15002)]
    [InlineData(ErrorType.Internal, 29001)]
    [InlineData(ErrorType.External, 59001)]
    [InlineData(ErrorType.Unavailable, 59002)]
    public void Factory_WithMatchingCode_ReturnsErrorWithCodeMessageAndType(ErrorType type, int errorCode)
    {
        var error = Create(type, errorCode, Message);

        error.Code.Should().Be(errorCode);
        error.Message.Should().Be(Message);
        error.Type.Should().Be(type);
    }

    [Theory]
    [InlineData(ErrorType.Validation, 1001)]
    [InlineData(ErrorType.Validation, 1999)]
    [InlineData(ErrorType.Internal, 9999)]
    [InlineData(ErrorType.Validation, 11001)]
    [InlineData(ErrorType.Validation, 51001)]
    [InlineData(ErrorType.Unauthorized, 55001)]
    [InlineData(ErrorType.Validation, 91001)]
    [InlineData(ErrorType.Unavailable, 99999)]
    public void Factory_WithBoundaryCode_Succeeds(ErrorType type, int errorCode)
    {
        var error = Create(type, errorCode, Message);

        error.Code.Should().Be(errorCode);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1001)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999)]
    [InlineData(1000)]
    [InlineData(100000)]
    [InlineData(101001)]
    [InlineData(int.MaxValue)]
    public void Factory_WithCodeOutOfRange_ThrowsArgumentOutOfRangeException(int errorCode)
    {
        var act = () => Error.Validation(errorCode, Message);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("code");
    }

    [Theory]
    [InlineData(ErrorType.NotFound, 2000)]
    [InlineData(ErrorType.Validation, 21000)]
    [InlineData(ErrorType.Unavailable, 99000)]
    public void Factory_WithZeroSequence_ThrowsArgumentException(ErrorType type, int errorCode)
    {
        var act = () => Create(type, errorCode, Message);

        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Theory]
    [InlineData(ErrorType.NotFound, 21001)]
    [InlineData(ErrorType.Validation, 22001)]
    [InlineData(ErrorType.Conflict, 24001)]
    [InlineData(ErrorType.BusinessRule, 23001)]
    [InlineData(ErrorType.Unauthorized, 29001)]
    [InlineData(ErrorType.Internal, 25001)]
    [InlineData(ErrorType.External, 1001)]
    public void Factory_WithTypeDigitMismatch_ThrowsArgumentException(ErrorType type, int errorCode)
    {
        var act = () => Create(type, errorCode, Message);

        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    public static TheoryData<ErrorType, int> ReservedTypeDigitCases()
    {
        var data = new TheoryData<ErrorType, int>();
        foreach (var type in Enum.GetValues<ErrorType>().Where(type => type != ErrorType.None))
        {
            foreach (var code in new[] { 10001, 16001, 17001, 18001 })
            {
                data.Add(type, code);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ReservedTypeDigitCases))]
    public void Factory_WithReservedTypeDigit_ThrowsArgumentException(ErrorType type, int errorCode)
    {
        var act = () => Create(type, errorCode, Message);

        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Theory]
    [InlineData(61001)]
    [InlineData(71001)]
    [InlineData(81001)]
    [InlineData(89999)]
    public void Factory_WithReservedServiceDigit_ThrowsArgumentException(int errorCode)
    {
        var act = () => Create(ErrorType.Validation, errorCode, Message);

        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Fact]
    public void Factory_WithNullMessage_ThrowsArgumentNullException()
    {
        var act = () => Error.NotFound(22001, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("message");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Factory_WithBlankMessage_ThrowsArgumentException(string errorMessage)
    {
        var act = () => Error.NotFound(22001, errorMessage);

        act.Should().Throw<ArgumentException>().WithParameterName("message");
    }

    [Theory]
    [InlineData("직원을 찾을 수 없습니다.")]
    [InlineData("Emoji 🚨 포함")]
    [InlineData(" 앞뒤 공백 ")]
    public void Factory_WithUnicodeOrPaddedMessage_KeepsMessageAsIs(string errorMessage)
    {
        var error = Error.NotFound(22001, errorMessage);

        error.Message.Should().Be(errorMessage);
    }

    [Fact]
    public void Equals_WithSameCodeMessageAndType_ReturnsTrue()
    {
        var left = Error.Conflict(23001, Message);
        var right = Error.Conflict(23001, Message);

        var result = left.Equals(right);

        result.Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentMessage_ReturnsFalse()
    {
        var left = Error.Conflict(23001, Message);
        var right = Error.Conflict(23001, "다른 메시지");

        var result = left.Equals(right);

        result.Should().BeFalse();
    }

    [Fact]
    public void With_WithoutChanges_ReturnsEqualCopy()
    {
        var error = Error.BusinessRule(24001, Message);

        var copy = error with { };

        copy.Should().Be(error);
        copy.Should().NotBeSameAs(error);
    }

    private static Error Create(ErrorType type, int code, string message) =>
        type switch
        {
            ErrorType.Validation => Error.Validation(code, message),
            ErrorType.NotFound => Error.NotFound(code, message),
            ErrorType.Conflict => Error.Conflict(code, message),
            ErrorType.BusinessRule => Error.BusinessRule(code, message),
            ErrorType.Unauthorized => Error.Unauthorized(code, message),
            ErrorType.Forbidden => Error.Forbidden(code, message),
            ErrorType.Internal => Error.Internal(code, message),
            ErrorType.External => Error.External(code, message),
            ErrorType.Unavailable => Error.Unavailable(code, message),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
}
