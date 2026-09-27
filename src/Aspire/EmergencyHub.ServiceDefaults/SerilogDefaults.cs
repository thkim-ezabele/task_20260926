using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace EmergencyHub.ServiceDefaults;

/// <summary>
/// Serilog 공통 구성입니다(ADR-0020, logging-observability "등록과 설정").
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>싱크 · 수준은 설정의 <c>Serilog</c> 절(<c>ReadFrom.Configuration</c>)로, DI에 등록된 싱크 · 보강기는 <c>ReadFrom.Services</c>로 받습니다(테스트 수집 싱크 포함).</description></item>
/// <item><description>OTLP 싱크는 <see cref="OtlpEndpoint.IsConfigured"/>일 때만 붙이고 <c>OTEL_*</c> 환경 변수를 읽게 둡니다(<c>ignoreEnvironment: false</c>). 로그의 OTLP 경로는 이것 하나입니다.</description></item>
/// <item><description><see cref="ExceptionHandlerMiddlewareCategory"/>는 설정보다 뒤에서 끕니다(설정으로 다시 켤 수 없음, BL-075).</description></item>
/// </list>
/// </remarks>
public static class SerilogDefaults
{
    /// <summary>
    /// 프레임워크 <c>ExceptionHandlerMiddleware</c>의 로그 범주입니다. 원본 예외 메시지(제약 이름 · SQL · 값)를 Error로 남기므로 끕니다.
    /// </summary>
    /// <remarks>
    /// BuildingBlocks.Api의 같은 상수는 Microsoft.Extensions.Logging 필터용이고, Serilog는 그 필터를 따르지 않아 여기서 따로 끕니다.
    /// ServiceDefaults는 MigrationService도 참조하므로 BuildingBlocks.Api를 참조하지 않고 값을 따로 둡니다(두 값이 같은지는 단위 테스트로 확인).
    /// </remarks>
    public const string ExceptionHandlerMiddlewareCategory = "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware";

    /// <summary>공통 보강 속성 <c>Environment</c>의 이름입니다.</summary>
    public const string EnvironmentPropertyName = "Environment";

    /// <summary>Serilog 로거 구성을 적용합니다.</summary>
    /// <param name="logger">로거 구성.</param>
    /// <param name="services">DI 컨테이너(<c>ILogEventSink</c> 등 수집).</param>
    /// <param name="configuration">설정.</param>
    /// <param name="environment">호스트 환경.</param>
    /// <returns>같은 <paramref name="logger"/>.</returns>
    /// <exception cref="ArgumentNullException">인자 중 하나가 <see langword="null"/>인 경우.</exception>
    public static LoggerConfiguration Configure(
        LoggerConfiguration logger,
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        logger
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty(EnvironmentPropertyName, environment.EnvironmentName)
            .MinimumLevel.Override(ExceptionHandlerMiddlewareCategory, LevelAlias.Off);

        if (OtlpEndpoint.IsConfigured(configuration))
        {
            logger.WriteTo.OpenTelemetry(_ => { }, ignoreEnvironment: false);
        }

        return logger;
    }
}
