using System.Reflection;
using EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EmergencyHub.ServiceDefaults.UnitTests;

// S03-T03 · ADR-0011 · ADR-0020: AddServiceDefaults는 OTel 트레이스 · 메트릭(OTLP 엔드포인트가 없으면 exporter 미등록),
// 서비스 디스커버리, Http.Resilience를 등록하고 ASP.NET 없는 Worker 빌더에서도 동작한다.
[Trait("FR", "PRD-001/FR-03")]
public sealed class ServiceDefaultsExtensionsTests
{
    private static readonly Assembly OtlpExporterAssembly = typeof(OtlpExporterOptions).Assembly;

    [Fact]
    public void AddServiceDefaults_WorkerBuilder_ResolvesTracerAndMeterProviders()
    {
        var builder = TestHosts.CreateWorkerBuilder();

        builder.AddServiceDefaults().Should().BeSameAs(builder);

        using var host = builder.Build();
        host.Services.GetService<TracerProvider>().Should().NotBeNull();
        host.Services.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddServiceDefaults_OtlpEndpointConfigured_RegistersOtlpExporter()
    {
        var builder = TestHosts.CreateWorkerBuilder(new Dictionary<string, string?> { [OtlpEndpoint.ConfigurationKey] = "http://127.0.0.1:4317" });

        builder.AddServiceDefaults();

        builder.Services.Should().Contain(descriptor => References(descriptor, OtlpExporterAssembly));
    }

    [Fact]
    public void AddServiceDefaults_NoOtlpEndpoint_RegistersNoOtlpExporter()
    {
        var builder = TestHosts.CreateWorkerBuilder();

        builder.AddServiceDefaults();

        builder.Services.Should().NotContain(descriptor => References(descriptor, OtlpExporterAssembly));
    }

    [Fact]
    public void AddServiceDefaults_WhitespaceOtlpEndpoint_RegistersNoOtlpExporter()
    {
        var builder = TestHosts.CreateWorkerBuilder(new Dictionary<string, string?> { [OtlpEndpoint.ConfigurationKey] = "   " });

        builder.AddServiceDefaults();

        builder.Services.Should().NotContain(descriptor => References(descriptor, OtlpExporterAssembly));
    }

    [Fact]
    public void AddServiceDefaults_Called_RegistersServiceDiscoveryAndHttpResilience()
    {
        var builder = TestHosts.CreateWorkerBuilder();

        builder.AddServiceDefaults();

        builder.Services.Should().Contain(descriptor => References(descriptor, typeof(ServiceEndpointResolver).Assembly));
        builder.Services.Should().Contain(descriptor => References(descriptor, typeof(HttpStandardResilienceOptions).Assembly));
    }

    [Fact]
    public void AddServiceDefaults_NullBuilder_Throws()
    {
        var act = () => ServiceDefaultsExtensions.AddServiceDefaults<HostApplicationBuilder>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("http://localhost:4317", true)]
    public void IsConfigured_EndpointValue_ReturnsWhetherNonBlank(string? value, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [OtlpEndpoint.ConfigurationKey] = value })
            .Build();

        OtlpEndpoint.IsConfigured(configuration).Should().Be(expected);
    }

    // 등록이 어셈블리의 형식(서비스 · 구현 · 제네릭 인자 · 팩터리 선언 형식)을 쓰는지.
    private static bool References(ServiceDescriptor descriptor, Assembly assembly)
    {
        IEnumerable<Type?> types =
        [
            descriptor.ServiceType,
            .. descriptor.ServiceType.GenericTypeArguments,
            descriptor.IsKeyedService ? descriptor.KeyedImplementationType : descriptor.ImplementationType,
            (descriptor.IsKeyedService ? descriptor.KeyedImplementationInstance : descriptor.ImplementationInstance)?.GetType(),
            (descriptor.IsKeyedService ? (Delegate?)descriptor.KeyedImplementationFactory : descriptor.ImplementationFactory)?.Method.DeclaringType,
        ];

        return types.Any(type => type?.Assembly == assembly);
    }
}
