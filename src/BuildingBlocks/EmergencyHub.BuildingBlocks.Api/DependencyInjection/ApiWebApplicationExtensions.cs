using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Api.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace EmergencyHub.BuildingBlocks.Api.DependencyInjection;

/// <summary>
/// 공통 API 처리의 요청 파이프라인 진입점입니다(ADR-0024 공개 진입점 2). <c>AddBuildingBlocksApi</c>와 짝으로 씁니다.
/// </summary>
public static class ApiWebApplicationExtensions
{
    /// <summary>
    /// 전역 예외 처리 미들웨어, 415 상태 코드 응답 변환, (Development에서만) OpenAPI 문서 · Swagger UI, Controller 엔드포인트를 붙입니다.
    /// </summary>
    /// <param name="app">웹 애플리케이션.</param>
    /// <returns>같은 <paramref name="app"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/>이 <see langword="null"/>인 경우.</exception>
    /// <remarks>
    /// <para>
    /// 예외 처리 미들웨어는 파이프라인 맨 앞에 두어야 뒤의 모든 예외를 받습니다. 전역 예외 처리기(<c>IExceptionHandler</c>)가 처리하지 못한 경우의
    /// 마지막 응답도 9001입니다. 분류된 오류가 404여도 그대로 응답합니다(<c>AllowStatusCode404Response</c>).
    /// </para>
    /// <para>
    /// OpenAPI JSON(<c>/swagger/v1/swagger.json</c>)과 Swagger UI(<c>/swagger</c>)는 Development 환경에서만 매핑합니다(ADR-0019).
    /// 인증 · 권한 미들웨어가 생기면(Identity 토픽) 예외 처리 뒤, 엔드포인트 앞에 둡니다.
    /// </para>
    /// </remarks>
    public static WebApplication UseBuildingBlocksApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            AllowStatusCode404Response = true,
            ExceptionHandler = GlobalExceptionHandler.WriteFallbackAsync,
        });

        // 라우팅이 본문 없이 끝낸 [Consumes] 불일치 415에 1005 ProblemDetails를 쓴다. 415가 아닌 상태는 건드리지 않는다(ADR-0028).
        app.UseStatusCodePages(UnsupportedMediaTypeStatusCodeResponses.WriteAsync);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint(
                $"/swagger/{ApiServiceCollectionExtensions.DocumentName}/swagger.json",
                ApiServiceCollectionExtensions.DocumentName));
        }

        app.MapControllers();
        return app;
    }
}
