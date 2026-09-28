using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EmergencyHub.Employee.Api.Employees.Import;

/// <summary>
/// 이 액션에서 폼 값 공급자(<see cref="FormValueProviderFactory"/> · <see cref="FormFileValueProviderFactory"/> · <see cref="JQueryFormValueProviderFactory"/>)를 뺍니다.
/// </summary>
/// <remarks>
/// <para>
/// 폼 값 공급자는 모델 바인딩 전에 <c>ReadFormAsync</c>로 본문을 읽습니다. 그러면 (1) 폼 한도 초과(<c>InvalidDataException</c>)가
/// <c>ValueProviderException</c> → ModelState → 400 · 1001이 되고(S06-T05 실측, 413이 아님), (2) 폼 필드를 문자열로 해독해 잘못된 UTF-8 바이트가 바뀝니다.
/// 이 특성을 붙인 액션은 전용 바인더(<see cref="EmployeeImportPayloadBinder"/>)만 본문을 읽습니다.
/// </para>
/// <para>리소스 필터라 값 공급자를 만들기 전(모델 바인딩 전)에 실행됩니다.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class DisableFormValueProvidersAttribute : Attribute, IResourceFilter
{
    /// <inheritdoc/>
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.ValueProviderFactories.RemoveType<FormValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<FormFileValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<JQueryFormValueProviderFactory>();
    }

    /// <inheritdoc/>
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
