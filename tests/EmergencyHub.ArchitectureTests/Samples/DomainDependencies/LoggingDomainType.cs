using Microsoft.Extensions.Logging;

namespace EmergencyHub.ArchitectureTests.Samples.DomainDependencies;

/// <summary>위반 예: 프레임워크 패키지(Microsoft.Extensions.Logging)를 주입받는다.</summary>
/// <param name="logger">로거.</param>
public sealed class LoggingDomainType(ILogger<LoggingDomainType> logger)
{
    /// <summary>로거.</summary>
    public ILogger<LoggingDomainType> Logger { get; } = logger;
}
