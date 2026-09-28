using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;

namespace EmergencyHub.BuildingBlocks.Api.Errors;

/// <summary>
/// 오류 유형(<see cref="ErrorType"/>) → HTTP 상태 코드 대응입니다(원본: wiki/05-api/error-codes.md "에러 코드 체계" 유형 표).
/// </summary>
/// <remarks>
/// HTTP 상태는 코드의 유형 자리(T)가 아니라 <see cref="ErrorType"/>으로 정합니다. T = 1 · 5 · 9는 상태가 둘 이상이기 때문입니다(T = 1은 400 · 413 · 415, ADR-0028).
/// </remarks>
public static class ErrorStatusCodes
{
    /// <summary>오류 유형에 대응하는 HTTP 상태 코드를 돌려줍니다.</summary>
    /// <param name="type">오류 유형.</param>
    /// <returns>HTTP 상태 코드. 예약 값 <see cref="ErrorType.None"/>과 정의되지 않은 값은 프로그래밍 오류라 <c>500</c>입니다.</returns>
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.PayloadTooLarge => StatusCodes.Status413PayloadTooLarge,
        ErrorType.UnsupportedMediaType => StatusCodes.Status415UnsupportedMediaType,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Internal => StatusCodes.Status500InternalServerError,
        ErrorType.External => StatusCodes.Status502BadGateway,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError,
    };
}
