using Microsoft.AspNetCore.Http;

namespace EmergencyHub.ArchitectureTests.Samples.InfrastructureAspNetCoreDependencies;

/// <summary>위반 예: ASP.NET Core 형식을 매개변수로 받는다.</summary>
public sealed class HttpContextInfrastructureType
{
    /// <summary>표본 메서드.</summary>
    /// <param name="context">HTTP 컨텍스트.</param>
    /// <returns>추적 ID.</returns>
    public static string Trace(HttpContext context) => context.TraceIdentifier;
}
