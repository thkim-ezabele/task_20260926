using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.BuildingBlocks.Api.Errors;

/// <summary>
/// 실패 <c>Result</c>의 <see cref="Error"/>를 <c>ProblemDetails</c> 응답으로 쓰는 <see cref="ActionResult"/>입니다(ADR-0016 "응답").
/// Controller는 <c>return result.Error.ToProblemResult();</c>로 씁니다. <c>ActionResult&lt;T&gt;</c>로 암시적 변환됩니다.
/// </summary>
/// <remarks>
/// <c>traceId</c>가 실행 시점의 요청 · <see cref="System.Diagnostics.Activity"/>를 따라야 하므로 <c>ProblemDetails</c>는
/// 실행할 때(<see cref="ExecuteResultAsync"/>) 만듭니다. 직렬화는 MVC 출력 포맷터(System.Text.Json, 정수 enum)가 합니다.
/// </remarks>
public sealed class ErrorProblemResult : ActionResult
{
    /// <summary>실패 원인으로 결과를 만듭니다.</summary>
    /// <param name="error">실패 원인.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    public ErrorProblemResult(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    /// <summary>실패 원인입니다.</summary>
    public Error Error { get; }

    /// <inheritdoc/>
    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problem = ErrorProblemDetails.Create(Error, context.HttpContext);
        var result = new ObjectResult(problem) { StatusCode = problem.Status };
        result.ContentTypes.Add(ErrorProblemDetails.ContentType);
        return result.ExecuteResultAsync(context);
    }
}
