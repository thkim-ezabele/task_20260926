using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace EmergencyHub.BuildingBlocks.Api.Exceptions;

/// <summary>
/// 처리되지 않은 예외를 <c>ProblemDetails</c>로 응답하는 전역 예외 처리기입니다(ADR-0016, ADR-0024 "Infrastructure 예외 분류 경로").
/// </summary>
/// <remarks>
/// <para>판정 순서:</para>
/// <list type="number">
/// <item><description><see cref="BadHttpRequestException"/>(요청 본문 · 헤더를 읽지 못함) → 400, 1001(<see cref="CommonErrors.ValidationFailed"/>). 로그 302(Information).</description></item>
/// <item><description>클라이언트가 요청을 끊어 난 취소 · 입출력 예외 → 판정은 9001 그대로, 로그 수준만 낮춤(303, Information). 본문은 끊긴 연결이라 쓰지 않습니다.</description></item>
/// <item><description>등록된 <see cref="IExceptionClassifier"/>를 등록 순서대로 묻고 처음 나온 오류로 응답. 로그 301(Warning). 예: 재시도 한도 초과 → 9003.</description></item>
/// <item><description>모두 <see langword="null"/>이면(분류기가 0개여도) 500, 9001(<see cref="CommonErrors.Unexpected"/>). 로그 이벤트 ID 1(Error).</description></item>
/// </list>
/// <para>
/// 형식 판별은 형식 검사로만 하고 형식 이름 문자열을 비교하지 않습니다. Infrastructure 형식(EF Core · Npgsql)은 분류기가 판별합니다.
/// 응답에는 예외 메시지 · 스택을 넣지 않고 오류의 고정 메시지만 넣습니다. 로그에는 메시지를 뺀 예외 사본(<see cref="RedactedException"/>)을 넘깁니다.
/// </para>
/// <para>상태가 없어 Singleton으로 등록합니다. 분류기도 Singleton이어야 합니다(Infrastructure 분류기는 Singleton, S02-T07).</para>
/// </remarks>
internal sealed class GlobalExceptionHandler(
    IEnumerable<IExceptionClassifier> classifiers,
    IOptions<HttpJsonOptions> jsonOptions,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly IExceptionClassifier[] _classifiers = [.. classifiers];

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (httpContext.Response.HasStarted)
        {
            // 이미 보낸 응답은 바꿀 수 없다. 미들웨어가 원래 예외를 다시 던진다.
            return false;
        }

        var aborted = IsRequestAborted(httpContext, exception);
        var error = Classify(httpContext, exception, aborted);

        await WriteAsync(httpContext, ErrorProblemDetails.Create(error, httpContext), jsonOptions.Value, aborted ? CancellationToken.None : cancellationToken);
        return true;
    }

    /// <summary>
    /// 전역 예외 처리기가 처리하지 않은 경우의 마지막 응답입니다(<c>ExceptionHandlerOptions.ExceptionHandler</c>). 항상 9001입니다.
    /// </summary>
    /// <param name="httpContext">현재 요청.</param>
    /// <returns>쓰기 작업.</returns>
    public static Task WriteFallbackAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        return WriteAsync(httpContext, ErrorProblemDetails.Create(CommonErrors.Unexpected, httpContext), new HttpJsonOptions(), httpContext.RequestAborted);
    }

    private static bool IsRequestAborted(HttpContext httpContext, Exception exception) =>
        exception is OperationCanceledException or IOException
        && exception is not BadHttpRequestException
        && httpContext.RequestAborted.IsCancellationRequested;

    private static Task WriteAsync(HttpContext httpContext, ProblemDetails problem, HttpJsonOptions options, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // 클라이언트가 끊은 요청은 토큰이 이미 취소되어 있다. CancellationToken.None을 넘기면 WriteAsJsonAsync가 RequestAborted 기준으로
        // 쓰기를 건너뛰고 OperationCanceledException을 삼킨다(취소된 토큰을 넘기면 처리기 밖으로 예외가 나감).
        return httpContext.Response.WriteAsJsonAsync(problem, options.SerializerOptions, ErrorProblemDetails.ContentType, cancellationToken);
    }

    private Error Classify(HttpContext httpContext, Exception exception, bool aborted)
    {
        var exceptionType = exception.GetType().FullName ?? exception.GetType().Name;

        if (exception is BadHttpRequestException badRequest)
        {
            logger.BadHttpRequestRejected(badRequest.StatusCode, CommonErrors.ValidationFailed.Code);
            return CommonErrors.ValidationFailed;
        }

        if (aborted)
        {
            logger.RequestAborted(exceptionType, CommonErrors.Unexpected.Code);
            return CommonErrors.Unexpected;
        }

        foreach (var classifier in _classifiers)
        {
            if (classifier.Classify(exception) is { } classified)
            {
                logger.ExceptionClassified(RedactedException.From(exception), exceptionType, classified.Code, (short)classified.Type);
                return classified;
            }
        }

        logger.UnhandledException(RedactedException.From(exception), exceptionType, httpContext.Request.Method, CommonErrors.Unexpected.Code);
        return CommonErrors.Unexpected;
    }
}
