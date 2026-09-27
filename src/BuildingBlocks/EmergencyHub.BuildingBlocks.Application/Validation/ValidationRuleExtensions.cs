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

    /// <summary>
    /// 정의되지 않은 코드값을 1002(<see cref="CommonErrors.InvalidCode"/>)로 거부합니다(ADR-0008, ADR-0018 "정의되지 않은 코드값").
    /// </summary>
    /// <typeparam name="T">검증 대상 형식.</typeparam>
    /// <typeparam name="TEnum">코드값 enum 형식.</typeparam>
    /// <param name="rule">규칙 빌더.</param>
    /// <returns>규칙 빌더. 뒤에 <see cref="WithError{T, TProperty}"/>를 붙이면 서비스 코드로 바꿀 수 있습니다.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/>이 <see langword="null"/>인 경우.</exception>
    /// <remarks>
    /// <para>일반 enum은 정의된 멤버만 허용하고 0(예약 값 <c>None</c> / <c>Unknown</c>)은 정의되어 있어도 거부합니다.</para>
    /// <para>
    /// <c>[Flags]</c> enum은 정의된 비트의 조합을 허용하고(0 포함), 정의되지 않은 비트가 하나라도 있으면 거부합니다.
    /// 조합 규칙(예: 최소 하나)은 Aggregate / Value Object가 검증합니다.
    /// </para>
    /// <para>
    /// 내부적으로 <c>Must(...).WithError(CommonErrors.InvalidCode)</c>입니다. FluentValidation의 <c>IsInEnum()</c>은 일반 enum의 0을
    /// 허용하므로 쓰지 않습니다.
    /// </para>
    /// </remarks>
    public static IRuleBuilderOptions<T, TEnum> MustBeDefinedEnum<T, TEnum>(this IRuleBuilder<T, TEnum> rule)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(rule);

        return rule.Must(DefinedEnumValues<TEnum>.IsDefined).WithError(CommonErrors.InvalidCode);
    }
}
