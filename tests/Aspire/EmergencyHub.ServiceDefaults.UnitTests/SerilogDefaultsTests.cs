using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.ServiceDefaults.UnitTests.TestDoubles;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace EmergencyHub.ServiceDefaults.UnitTests;

// S03-T03 · ADR-0020 · BL-075: Serilog는 ReadFrom.Services로 DI의 ILogEventSink를 받고,
// ExceptionHandlerMiddleware 범주는 설정과 무관하게 꺼진다(Serilog는 Microsoft.Extensions.Logging 필터를 따르지 않음).
[Trait("FR", "PRD-001/FR-03")]
public sealed class SerilogDefaultsTests
{
    private const string SiblingCategory = "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware";

    [Fact]
    public void AddServiceDefaults_SinkRegisteredInDi_ReceivesILoggerEventsWithCommonProperties()
    {
        var sink = new CollectingSink();
        using var host = BuildHost(sink);

        host.Services.GetRequiredService<ILogger<SerilogDefaultsTests>>().Registered(42);

        var logEvent = sink.Events.Should().ContainSingle().Which;
        logEvent.Level.Should().Be(LogEventLevel.Information);
        logEvent.MessageTemplate.Text.Should().Be(SampleLogs.RegisteredTemplate);
        logEvent.Properties["EmployeeId"].ToString().Should().Be("42");
        logEvent.Properties[SerilogDefaults.EnvironmentPropertyName].ToString().Should().Be("\"Testing\"");
        logEvent.Properties.Should().ContainKey("MachineName");
        logEvent.Properties["SourceContext"].ToString().Should().Contain(nameof(SerilogDefaultsTests));
    }

    [Fact]
    public void AddServiceDefaults_ExceptionHandlerMiddlewareError_IsDropped()
    {
        var sink = new CollectingSink();
        using var host = BuildHost(sink);

        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(SerilogDefaults.ExceptionHandlerMiddlewareCategory);
        logger.ErrorSample(new InvalidOperationException("duplicate key value violates unique constraint"));
        logger.CriticalSample();

        sink.Events.Should().BeEmpty();
    }

    [Fact]
    public void AddServiceDefaults_ConfigurationReenablesExceptionHandlerMiddleware_StillDropped()
    {
        var sink = new CollectingSink();
        using var host = BuildHost(sink, new Dictionary<string, string?>
        {
            [$"Serilog:MinimumLevel:Override:{SerilogDefaults.ExceptionHandlerMiddlewareCategory}"] = "Verbose",
        });

        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(SerilogDefaults.ExceptionHandlerMiddlewareCategory)
            .ErrorSample(null);

        sink.Events.Should().BeEmpty();
    }

    [Fact]
    public void AddServiceDefaults_SiblingDiagnosticsCategory_IsNotDropped()
    {
        // 접두사가 같은 다른 범주까지 끄지 않는다(Serilog Override는 점 단위 접두사 비교).
        var sink = new CollectingSink();
        using var host = BuildHost(sink);

        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(SiblingCategory).ErrorSample(null);

        sink.Events.Should().ContainSingle().Which.Level.Should().Be(LogEventLevel.Error);
    }

    [Fact]
    public void AddServiceDefaults_MinimumLevelFromConfiguration_IsApplied()
    {
        var sink = new CollectingSink();
        using var host = BuildHost(sink, new Dictionary<string, string?> { ["Serilog:MinimumLevel:Default"] = "Warning" });

        var logger = host.Services.GetRequiredService<ILogger<SerilogDefaultsTests>>();
        logger.InformationSample();
        logger.WarningSample();

        sink.Events.Should().ContainSingle().Which.Level.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void AddServiceDefaults_LoggerProviders_HaveNoOpenTelemetryProvider()
    {
        // ADR-0020 중복 방지 1: OpenTelemetry SDK 로그 공급자를 등록하지 않는다(로그 OTLP 경로는 Serilog 싱크 하나).
        using var host = BuildHost(new CollectingSink(), new Dictionary<string, string?> { [OtlpEndpoint.ConfigurationKey] = "http://127.0.0.1:1" });

        var providers = host.Services.GetServices<ILoggerProvider>().ToList();

        providers.Should().NotContain(provider => provider.GetType().Namespace!.StartsWith("OpenTelemetry", StringComparison.Ordinal));
    }

    [Fact]
    public void ExceptionHandlerMiddlewareCategory_MatchesFrameworkTypeAndBuildingBlocksApiFilter()
    {
        SerilogDefaults.ExceptionHandlerMiddlewareCategory.Should().Be(typeof(ExceptionHandlerMiddleware).FullName);
        SerilogDefaults.ExceptionHandlerMiddlewareCategory.Should().Be(ApiServiceCollectionExtensions.ExceptionHandlerMiddlewareCategory);
    }

    private static IHost BuildHost(ILogEventSink sink, IDictionary<string, string?>? settings = null)
    {
        var builder = TestHosts.CreateWorkerBuilder(settings);
        builder.Services.AddSingleton(sink);
        builder.AddServiceDefaults();
        return builder.Build();
    }
}
