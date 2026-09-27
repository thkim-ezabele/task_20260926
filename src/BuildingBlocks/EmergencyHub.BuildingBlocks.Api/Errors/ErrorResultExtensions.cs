using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Api.Errors;

/// <summary>
/// Controller에서 실패 <c>Result</c>를 응답으로 바꾸는 확장입니다(ADR-0016).
/// </summary>
public static class ErrorResultExtensions
{
    /// <summary>오류를 <c>ProblemDetails</c> 응답(<see cref="ErrorProblemResult"/>)으로 바꿉니다.</summary>
    /// <param name="error">실패 원인. 보통 <c>result.Error</c>입니다.</param>
    /// <returns>응답 결과.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    public static ErrorProblemResult ToProblemResult(this Error error) => new(error);
}
