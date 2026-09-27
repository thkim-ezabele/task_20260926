using Microsoft.AspNetCore.Http;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationFrameworkDependencies;

/// <summary>위반 예: ASP.NET Core 형식을 쓴다.</summary>
public sealed class AspNetCoreApplicationType
{
    /// <summary>HTTP 컨텍스트.</summary>
    public HttpContext? Context { get; init; }
}
