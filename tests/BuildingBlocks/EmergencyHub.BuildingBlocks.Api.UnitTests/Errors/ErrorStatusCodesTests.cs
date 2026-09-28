using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Errors;

// ErrorType → HTTP 상태 전수 대응. 원본은 wiki/05-api/error-codes.md "에러 코드 체계" 유형 표(HTTP 상태는 T가 아니라 ErrorType으로 정함).
public sealed class ErrorStatusCodesTests
{
    public static TheoryData<ErrorType, int> DocumentedStatuses() => new()
    {
        { ErrorType.Validation, StatusCodes.Status400BadRequest },
        { ErrorType.PayloadTooLarge, StatusCodes.Status413PayloadTooLarge },
        { ErrorType.UnsupportedMediaType, StatusCodes.Status415UnsupportedMediaType },
        { ErrorType.NotFound, StatusCodes.Status404NotFound },
        { ErrorType.Conflict, StatusCodes.Status409Conflict },
        { ErrorType.BusinessRule, StatusCodes.Status422UnprocessableEntity },
        { ErrorType.Unauthorized, StatusCodes.Status401Unauthorized },
        { ErrorType.Forbidden, StatusCodes.Status403Forbidden },
        { ErrorType.Internal, StatusCodes.Status500InternalServerError },
        { ErrorType.External, StatusCodes.Status502BadGateway },
        { ErrorType.Unavailable, StatusCodes.Status503ServiceUnavailable },
    };

    // ---- 성공 ----

    [Theory]
    [MemberData(nameof(DocumentedStatuses))]
    public void ToStatusCode_DocumentedErrorType_ReturnsDocumentedStatus(ErrorType type, int expected)
    {
        type.ToStatusCode().Should().Be(expected);
    }

    [Fact]
    public void DocumentedStatuses_CoverEveryDefinedErrorTypeExceptNone()
    {
        // ErrorType에 멤버가 추가되면 이 표(와 error-codes.md)를 함께 갱신해야 한다.
        var covered = DocumentedStatuses().Select(row => row.Data.Item1);

        covered.Should().BeEquivalentTo(Enum.GetValues<ErrorType>().Where(type => type != ErrorType.None));
    }

    // ---- 실패 / 엣지 ----

    [Fact]
    public void ToStatusCode_None_Returns500()
    {
        // None은 Error에 쓰지 않는 예약 값이다. 들어오면 프로그래밍 오류라 서버 오류로 응답한다.
        ErrorType.None.ToStatusCode().Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [InlineData((short)13)]
    [InlineData((short)19)]
    [InlineData((short)50)]
    [InlineData((short)99)]
    [InlineData((short)-1)]
    [InlineData(short.MaxValue)]
    public void ToStatusCode_UndefinedValue_Returns500(short value)
    {
        ((ErrorType)value).ToStatusCode().Should().Be(StatusCodes.Status500InternalServerError);
    }
}
