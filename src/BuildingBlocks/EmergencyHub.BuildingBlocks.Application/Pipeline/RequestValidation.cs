using EmergencyHub.BuildingBlocks.Domain.Errors;
using FluentValidation;
using FluentValidation.Results;

namespace EmergencyHub.BuildingBlocks.Application.Pipeline;

/// <summary>
/// Command · Query 검증 데코레이터가 함께 쓰는 검증 실행 · 변환 도우미입니다(ADR-0018).
/// </summary>
internal static class RequestValidation
{
    /// <summary>
    /// Validator를 등록 순서대로 모두 실행해 실패를 모읍니다.
    /// </summary>
    /// <typeparam name="TRequest">요청 형식.</typeparam>
    /// <param name="validators">요청 형식의 Validator 목록. 비어 있을 수 있습니다.</param>
    /// <param name="request">검증할 요청.</param>
    /// <param name="cancellationToken">취소 토큰. 모든 Validator에 그대로 전달합니다.</param>
    /// <returns>실패가 하나라도 있으면 <see cref="ValidationError"/>, 없으면 <see langword="null"/>.</returns>
    public static async Task<ValidationError?> ValidateAsync<TRequest>(
        IEnumerable<IValidator<TRequest>> validators,
        TRequest request,
        CancellationToken cancellationToken)
    {
        List<FieldError>? fieldErrors = null;

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            foreach (var failure in result.Errors)
            {
                (fieldErrors ??= []).Add(ToFieldError(failure));
            }
        }

        return fieldErrors is null ? null : ValidationError.Create(fieldErrors);
    }

    /// <summary>
    /// FluentValidation 실패 하나를 필드별 상세로 바꿉니다.
    /// </summary>
    /// <remarks>
    /// <para><c>CustomState</c>에 <see cref="Error"/>가 있으면(<c>WithError</c>) 그 코드 · 메시지를 씁니다.</para>
    /// <para>
    /// 없으면 1001(<see cref="CommonErrors.ValidationFailed"/>) 코드에 실패 메시지를 담아 감쌉니다(BL-053).
    /// 메시지가 비어 있으면 1001의 기본 메시지를 씁니다. 속성 경로가 <see langword="null"/>이면 객체 수준 규칙으로 보고 빈 문자열을 씁니다.
    /// </para>
    /// <para>
    /// 검증 실패 유형이 아닌 <see cref="Error"/>를 실은 규칙은 잘못 정의된 규칙(프로그래밍 오류)이라
    /// <see cref="FieldError.Create"/>가 <see cref="ArgumentException"/>을 던집니다.
    /// </para>
    /// </remarks>
    private static FieldError ToFieldError(ValidationFailure failure)
    {
        var error = failure.CustomState as Error ?? WrapAsValidationFailed(failure.ErrorMessage);
        return FieldError.Create(failure.PropertyName ?? string.Empty, error);
    }

    private static Error WrapAsValidationFailed(string? message) =>
        string.IsNullOrWhiteSpace(message)
            ? CommonErrors.ValidationFailed
            : Error.Validation(CommonErrors.ValidationFailed.Code, message);
}
