using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Application.Validation;

/// <summary>
/// Command / Query Validator의 공통 기반입니다. 한 속성의 규칙 체인은 첫 실패에서 멈추고(<see cref="CascadeMode.Stop"/>),
/// 여러 속성의 실패는 모두 모읍니다(ADR-0018 "중단 방식").
/// </summary>
/// <typeparam name="TRequest">검증할 요청 형식.</typeparam>
/// <remarks>
/// <para>
/// 서비스 Validator는 <c>internal sealed class RegisterEmployeeCommandValidator : RequestValidator&lt;RegisterEmployeeCommand&gt;</c>로 만들고,
/// 규칙마다 <see cref="ValidationRuleExtensions.WithError{T, TProperty}"/>로 정수 코드를 붙입니다.
/// </para>
/// <para>
/// 전역 기본값(<c>ValidatorOptions.Global</c>) 대신 기반 클래스에 둔 이유: 정적 전역 설정은 DI 등록을 거치지 않는 Validator 단위 테스트와
/// 실제 호스트의 동작이 달라질 수 있습니다. 기반 클래스는 생성 시점에 정해져 어디서 만들어도 같습니다.
/// Validator는 DB에 접근하지 않습니다(입력 형식 · 범위만).
/// </para>
/// </remarks>
public abstract class RequestValidator<TRequest> : AbstractValidator<TRequest>
{
    /// <summary>규칙 수준 중단 방식을 <see cref="CascadeMode.Stop"/>으로 정합니다. 파생 생성자의 규칙 정의보다 먼저 실행됩니다.</summary>
    protected RequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
    }
}
