using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Api.Validation;

/// <summary>
/// <c>[ApiController]</c>의 자동 400(<see cref="ApiBehaviorOptions.InvalidModelStateResponseFactory"/>)을
/// 1001(<see cref="CommonErrors.ValidationFailed"/>) <c>ProblemDetails</c>로 바꿉니다(ADR-0016 "바인딩 오류").
/// </summary>
/// <remarks>
/// <para>
/// 모델 바인딩은 JSON 형식 · 형식 변환 오류만 맡습니다(필수 값 · 범위 · 코드값은 FluentValidation, ADR-0018). 그래서 필드마다 코드는 1001입니다.
/// </para>
/// <para>
/// 필드별 메시지는 1001의 고정 문구로 바꿉니다. 프레임워크 메시지에는 입력 값(<c>The value 'hong@example.com' is not valid</c>)과
/// 내부 형식 이름이 들어 있기 때문입니다(api-guidelines "detail에 개인정보 · 내부 구현을 넣지 않는다").
/// </para>
/// </remarks>
internal static class InvalidModelStateResponses
{
    /// <summary>모델 상태 오류로 400 응답을 만듭니다.</summary>
    /// <param name="context">액션 컨텍스트.</param>
    /// <returns>400 <c>application/problem+json</c> 응답.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/>가 <see langword="null"/>인 경우.</exception>
    public static IActionResult Create(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<FieldError> fieldErrors = [];
        foreach (var (key, entry) in context.ModelState)
        {
            foreach (var _ in entry.Errors)
            {
                fieldErrors.Add(FieldError.Create(FieldErrorKeys.FromModelStateKey(key), CommonErrors.ValidationFailed));
            }
        }

        if (fieldErrors.Count == 0)
        {
            // 오류 항목 없이 무효로 표시된 경우. ValidationError는 필드 1개 이상이 필요하므로 객체 수준 항목 하나를 둔다.
            fieldErrors.Add(FieldError.Create(string.Empty, CommonErrors.ValidationFailed));
        }

        var error = ValidationError.Create(fieldErrors);
        LogBindingFailure(context, fieldErrors);

        var result = new ObjectResult(ErrorProblemDetails.Create(error, context.HttpContext)) { StatusCode = error.Type.ToStatusCode() };
        result.ContentTypes.Add(ErrorProblemDetails.ContentType);
        return result;
    }

    private static void LogBindingFailure(ActionContext context, List<FieldError> fieldErrors)
    {
        var logger = context.HttpContext.RequestServices?.GetService<ILoggerFactory>()?.CreateLogger(typeof(InvalidModelStateResponses).FullName!);
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        // 필드 이름만 남긴다(값 · 메시지 없음).
        var fieldNames = string.Join(", ", fieldErrors.Select(fieldError => fieldError.PropertyName).Distinct(StringComparer.Ordinal));
        logger.ModelBindingFailed(fieldNames, CommonErrors.ValidationFailed.Code);
    }
}
