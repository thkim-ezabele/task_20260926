using EmergencyHub.BuildingBlocks.Domain.Errors;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.Validation;

/// <summary>
/// FluentValidation 규칙에 정수 에러 코드를 붙이는 확장입니다(ADR-0008, ADR-0018).
/// </summary>
public static class ValidationRuleExtensions
{
    /// <summary>
    /// 규칙이 실패하면 <paramref name="error"/>의 코드 · 메시지로 보고합니다.
    /// <c>WithState(_ =&gt; error)</c>로 <see cref="Error"/>를 <c>CustomState</c>에 싣고 <c>WithMessage(error.Message)</c>로 메시지를 맞춥니다.
    /// </summary>
    /// <typeparam name="T">검증 대상 형식.</typeparam>
    /// <typeparam name="TProperty">속성 형식.</typeparam>
    /// <param name="rule">바로 앞 규칙.</param>
    /// <param name="error">검증 실패 유형(<see cref="ErrorType.Validation"/>)의 오류.</param>
    /// <returns>같은 규칙 빌더.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> 또는 <paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException"><paramref name="error"/>의 유형이 검증 실패가 아닌 경우(필드별 상세는 검증 실패 유형만 담음).</exception>
    /// <remarks>
    /// FluentValidation의 문자열 <c>ErrorCode</c>는 쓰지 않습니다. 이 확장을 붙이지 않은 규칙의 실패는 1001로 담깁니다.
    /// </remarks>
    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Error error)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(error);

        if (error.Type != ErrorType.Validation)
        {
            throw new ArgumentException(
                $"검증 규칙에는 검증 실패 유형의 오류만 붙입니다. 받은 오류: {error.Code}, 유형: ErrorType.{error.Type}", nameof(error));
        }

        return rule.WithState(_ => error).WithMessage(error.Message);
    }
}
