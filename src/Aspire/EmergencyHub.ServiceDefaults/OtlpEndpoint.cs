using Microsoft.Extensions.Configuration;

namespace EmergencyHub.ServiceDefaults;

/// <summary>
/// OTLP 내보내기 여부 판단입니다. Aspire가 <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>를 주입할 때만 트레이스 · 메트릭 exporter와 Serilog OTLP 싱크를 붙입니다(ADR-0020).
/// </summary>
/// <remarks>테스트 · CI 호스트에는 값이 없으므로 수집기 연결을 시도하지 않습니다.</remarks>
public static class OtlpEndpoint
{
    /// <summary>OTLP 엔드포인트 설정 키(환경 변수 이름)입니다.</summary>
    public const string ConfigurationKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>OTLP 엔드포인트가 설정되어 있는지 확인합니다.</summary>
    /// <param name="configuration">설정.</param>
    /// <returns>값이 있고 공백만이 아니면 <see langword="true"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/>이 <see langword="null"/>인 경우.</exception>
    public static bool IsConfigured(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return !string.IsNullOrWhiteSpace(configuration[ConfigurationKey]);
    }
}
