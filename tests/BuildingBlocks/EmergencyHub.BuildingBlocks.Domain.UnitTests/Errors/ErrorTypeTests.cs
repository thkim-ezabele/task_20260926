using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Errors;

public sealed class ErrorTypeTests
{
    [Theory]
    [InlineData(ErrorType.None, 0)]
    [InlineData(ErrorType.Validation, 10)]
    [InlineData(ErrorType.PayloadTooLarge, 11)]
    [InlineData(ErrorType.UnsupportedMediaType, 12)]
    [InlineData(ErrorType.NotFound, 20)]
    [InlineData(ErrorType.Conflict, 30)]
    [InlineData(ErrorType.BusinessRule, 40)]
    [InlineData(ErrorType.Unauthorized, 51)]
    [InlineData(ErrorType.Forbidden, 52)]
    [InlineData(ErrorType.Internal, 91)]
    [InlineData(ErrorType.External, 92)]
    [InlineData(ErrorType.Unavailable, 93)]
    public void Value_ForEachMember_MatchesDocumentedTwoDigitValue(ErrorType type, short expected)
    {
        var value = (short)type;

        value.Should().Be(expected);
    }

    [Fact]
    public void GetValues_Always_ContainsOnlyDocumentedMembers()
    {
        var values = Enum.GetValues<ErrorType>();

        values.Should().HaveCount(12);
    }

    [Fact]
    public void GetValues_ExceptNone_HaveTypeDigitInDocumentedRange()
    {
        var typeDigits = Enum.GetValues<ErrorType>()
            .Where(type => type != ErrorType.None)
            .Select(type => (short)type / 10);

        typeDigits.Should().OnlyContain(digit => digit == 1 || digit == 2 || digit == 3 || digit == 4 || digit == 5 || digit == 9);
    }
}
